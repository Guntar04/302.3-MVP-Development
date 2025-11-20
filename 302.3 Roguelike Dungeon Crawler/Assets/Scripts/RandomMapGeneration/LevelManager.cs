using System;
using System.Collections;
using System.Linq;
using System.Reflection; // <-- added
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Required for TextMeshPro
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public int currentFloor = 1;

    [Header("Game Progression")]
public string WinMenu = "WinMenu"; // Set this to your Win screen scene name
public int finalLevel = 5;

    [Header("Player spawn waiting")]
    public float playerSpawnWaitTime = 1.0f;

    [Header("Events")]
    public UnityEvent<int> OnBeforeFloorUnload;
    public UnityEvent<int> OnAfterFloorLoad;

    [Header("Level UI")]
    public GameObject nextLevelUI; // (optional) Reference to the NextLevelUI GameObject (kept for backwards compatibility)
    public TextMeshProUGUI levelText; // Reference to the TextMeshPro component
    [Tooltip("Seconds to show the Next Level UI when transitioning floors.")]
    public float levelUIDisplaySeconds = 3f;

    [Header("Player Stats")]
public int enemiesKilled = 0;

// Call this whenever an enemy dies
public void RegisterEnemyKill()
{
    enemiesKilled++;
    Debug.Log($"Enemy killed! Total: {enemiesKilled}");
}


    // cached controller (optional). If present, we'll call ShowTemporary on it instead of manually toggling GameObject.
    private NextLevelUIController nextLevelUIController;
    // helper to lazily resolve references in the just-loaded scene
    private void EnsureUIReferences()
    {
        if (nextLevelUIController != null && levelText != null) return;

        if (nextLevelUI != null && nextLevelUIController == null)
            nextLevelUIController = nextLevelUI.GetComponent<NextLevelUIController>();

        if (nextLevelUIController == null)
            nextLevelUIController = FindFirstObjectByType<NextLevelUIController>();

        if (nextLevelUIController != null && nextLevelUI == null)
            nextLevelUI = nextLevelUIController.gameObject;

        if (levelText == null)
        {
            if (nextLevelUI != null)
                levelText = nextLevelUI.GetComponentInChildren<TextMeshProUGUI>(true);

            if (levelText == null)
            {
                // fallback: try to find a TMP with "level" in its name
                var all = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
                foreach (var t in all)
                {
                    if (t == null) continue;
                    if (t.name.IndexOf("level", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        levelText = t;
                        break;
                    }
                }
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // try to find controller on assigned GameObject
        if (nextLevelUI != null)
            nextLevelUIController = nextLevelUI.GetComponent<NextLevelUIController>();

        // show at startup briefly if desired (uses same path as transitions)
        StartCoroutine(DisplayLevelUI(currentFloor));
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Call this before starting a fresh run to remove any leftover persistent objects
    public static void ResetPersistentGameState()
    {
        Debug.Log("ResetPersistentGameState: cleaning persistent objects...");

        // 1) Destroy the common persistent root (if your project uses a GameObject named "DontDestroyOnLoad")
        var ddolRoot = GameObject.Find("DontDestroyOnLoad");
        if (ddolRoot != null)
        {
            Debug.Log("ResetPersistentGameState: Destroying DontDestroyOnLoad root");
            UnityEngine.Object.Destroy(ddolRoot);
        }

        // 2) Destroy any leftover player GameObjects (tagged "Player")
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            Debug.Log($"ResetPersistentGameState: Destroying leftover player {p.name}");
            UnityEngine.Object.Destroy(p);
        }

        // 3) Destroy any runtime-created loot/ pickups etc that might not be children of the root
        //    (look for common name patterns and components)
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null) continue;

            // skip scene objects that are in a different scene asset (prefabs)
            if (!go.scene.isLoaded) continue;

            var name = go.name ?? "";

            bool shouldDestroyByName =
                name.StartsWith("Loot", System.StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("HealthPot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Chest", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Pickup", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (shouldDestroyByName)
            {
                Debug.Log($"ResetPersistentGameState: destroying by name {name}");
                UnityEngine.Object.Destroy(go);
                continue;
            }

            // or destroy objects that have components with type names indicating singletons/pickups
            var comps = go.GetComponents<Component>();
            if (comps != null)
            {
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    var tname = c.GetType().Name;
                    if (tname.IndexOf("Inventory", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tname.IndexOf("UIManager", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tname.IndexOf("PlayerStats", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tname.IndexOf("Loot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tname.IndexOf("Pickup", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.Log($"ResetPersistentGameState: destroying by component {go.name} ({tname})");
                        UnityEngine.Object.Destroy(go);
                        break;
                    }
                }
            }
        }

        // 4) Clear common singleton static Instance fields via reflection if present.
        string[] singletonTypeNames = { "UIManager", "PlayerStats", "InventoryUIController", "PlayerProgress", "LevelManager" };
        foreach (var typeName in singletonTypeNames)
        {
            try
            {
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes().Where(t => t.Name == typeName))
                    .FirstOrDefault();
                if (type == null) continue;
                var field = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(null, null);
                    Debug.Log($"ResetPersistentGameState: cleared static Instance on {typeName}");
                }
            }
            catch { /* ignore */ }
        }
    }

    public void GoToNextFloor(GameObject player)
    {
        StartCoroutine(NextFloorCoroutine(player));
    }

    private IEnumerator NextFloorCoroutine(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("LevelManager: GoToNextFloor called with null player.");
            yield break;
        }

        // before unloading/spawning next floor, save player state via PlayerProgress (robust)
        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            PlayerProgress.SaveFrom(pc);
            Debug.Log($"LevelManager: PlayerProgress saved (health={PlayerProgress.health}, max={PlayerProgress.maxHealth}, shields={PlayerProgress.shieldCount})");
        }

        // before unloading/spawning next floor, also save player state into a local int as before (fallback)
        int savedHealth = TryGetPlayerHealth(player);
        Debug.Log($"LevelManager: saving player health = {savedHealth}");

        // Clear player's key immediately (so it doesn't carry to next floor)
        TryClearPlayerKey(player);

        // Notify listeners
        OnBeforeFloorUnload?.Invoke(currentFloor);

        // Reset per-level data
        var dd = FindFirstObjectByType<DungeonData>();
        if (dd != null)
        {
            try { dd.Reset(); Debug.Log("LevelManager: DungeonData.Reset called."); }
            catch (Exception ex) { Debug.LogWarning($"LevelManager: DungeonData.Reset threw: {ex.Message}"); }
        }

        // Cleanup previous floor objects
        CleanupPreviousFloor(player);

        // Increment floor
        currentFloor++;
        Debug.Log($"LevelManager: Generating floor {currentFloor}...");

        if (currentFloor > finalLevel)
{
    Debug.Log("Player completed final level! Loading Win Screen...");
    UnityEngine.SceneManagement.SceneManager.LoadScene(WinMenu);
    yield break; // stop the coroutine
}

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.floorsCompleted = currentFloor;
            Debug.Log($"PlayerStats: floorsCompleted updated to {PlayerStats.Instance.floorsCompleted}");
        }

        // Small frame
        yield return null;

        // Invoke generator
        bool generated = TryInvokeGenerator();

        // Give generator a frame
        yield return null;

        // Fallback: call common spawners so player/enemies/chests/exit get placed reliably
        try
        {
            var ps = FindFirstObjectByType<PlayerSpawner>();
            if (ps != null)
            {
                var m = ps.GetType().GetMethod("PlacePlayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      ?? ps.GetType().GetMethod("PlacePlayerAtSpawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                InvokeMethodWithOptionalGameObject(m, ps, player);
            }

            var es = FindFirstObjectByType<EnemySpawner>();
            if (es != null)
            {
                var m = es.GetType().GetMethod("PlaceEnemies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? es.GetType().GetMethod("SpawnEnemies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                m?.Invoke(es, null);
            }

            var cs = FindFirstObjectByType<ChestSpawner>();
            if (cs != null)
            {
                var m = cs.GetType().GetMethod("SpawnChests", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                m?.Invoke(cs, null);
            }

            var xs = FindFirstObjectByType<ExitSpawner>();
            if (xs != null)
            {
                var m = xs.GetType().GetMethod("SpawnExit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                m?.Invoke(xs, null);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"LevelManager: fallback spawner invocation failed: {ex.Message}");
        }

        // give spawners a short moment
        yield return null;

        // WAIT for player to appear (generator/spawner may spawn asynchronously).
        float waited = 0f;
        GameObject newPlayer = null;
        while (waited < playerSpawnWaitTime)
        {
            newPlayer = GameObject.FindWithTag("Player");
            if (newPlayer == null)
            {
                var foundPc = FindFirstObjectByType<PlayerController>();
                if (foundPc != null) newPlayer = foundPc.gameObject;
            }

            if (newPlayer != null) break;

            waited += Time.deltaTime;
            yield return null;
        }

        if (newPlayer != null)
        {
            // Rebind shield UI to the new player
            if (UIManager.Instance != null)
            {
                UIManager.Instance.BindPlayer(newPlayer);
            }
            else
            {
                Debug.LogWarning("LevelManager: UIManager.Instance is null - Shield UI was not rebound.");
            }
        }

        // last-chance explicit spawn attempt
        if (newPlayer == null)
        {
            var ps = FindFirstObjectByType<PlayerSpawner>();
            if (ps != null)
            {
                var m = ps.GetType().GetMethod("PlacePlayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      ?? ps.GetType().GetMethod("PlacePlayerAtSpawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                try
                {
                    m?.Invoke(ps, null);
                }
                catch { }

                // wait a short while for it to appear
                float extraWait = 0f;
                while (extraWait < 0.5f)
                {
                    newPlayer = GameObject.FindWithTag("Player");
                    if (newPlayer == null)
                    {
                        var foundPc = FindFirstObjectByType<PlayerController>();
                        if (foundPc != null) newPlayer = foundPc.gameObject;
                    }
                    if (newPlayer != null) break;
                    extraWait += Time.deltaTime;
                    yield return null;
                }
            }
        }

        if (newPlayer == null)
        {
            Debug.LogWarning("LevelManager: couldn't find player instance to restore state to after generation.");
        }
        else
        {
            // first, try to restore via PlayerProgress (preferred)
            var newPcTyped = newPlayer.GetComponent<PlayerController>();
            if (newPcTyped != null && PlayerProgress.HasSaved)
            {
                PlayerProgress.ApplyTo(newPcTyped);
                Debug.Log($"LevelManager: applied PlayerProgress to new player (health={newPcTyped.health}, max={newPcTyped.maxHealth})");
            }
            else
            {
                // fallback: try set health from savedHealth
                bool restored = TrySetPlayerHealth(newPlayer, savedHealth);
                if (restored) Debug.Log($"LevelManager: restored player health = {savedHealth}");
            }

            // restore progress (existing)
            if (PlayerProgress.HasSaved)
            {
                PlayerProgress.ApplyTo(newPlayer.GetComponent<PlayerController>());
            }

            // Rebind UI to the newly spawned player so shield UI updates and survives player respawn
            if (UIManager.Instance != null)
            {
                UIManager.Instance.BindPlayer(newPlayer);
                Debug.Log("LevelManager: UIManager.BindPlayer called for new player.");
            }
            else
            {
                Debug.LogWarning("LevelManager: UIManager.Instance is null - HUD was not rebound to new player.");
            }
        }

        // clear key safety
        TryClearPlayerKey(newPlayer ?? player);

        // position player at spawn (safe routine)
        TryPlacePlayerAtSpawn(newPlayer ?? player);

        yield return null;

        OnAfterFloorLoad?.Invoke(currentFloor);
        Debug.Log($"LevelManager: floor {currentFloor} ready.");

        // show the UI after the new floor has been created and after OnAfterFloorLoad listeners ran
        StartCoroutine(DisplayLevelUI(currentFloor));
    }

    private IEnumerator DisplayLevelUI(int level)
    {
        // Resolve references (will try to find even inactive children via GetComponentInChildren(true))
        EnsureUIReferences();

        if (levelText == null)
        {
            Debug.LogWarning("LevelManager: LevelText is not assigned and could not be found in scene.");
            yield break;
        }

        levelText.text = $"{level:D2}";

        // Prefer using the robust global helper which can activate an inactive UI object
        NextLevelUIController.ShowTemporaryGlobal(levelUIDisplaySeconds);
        yield break;

        // --- kept for reference: older fallback logic removed ---
    }

    // New: destroys typical per-floor spawned objects so next floor starts clean
    private void CleanupPreviousFloor(GameObject player)
    {
        // destroy enemies (typed component)
        var enemies = UnityEngine.Object.FindObjectsByType<AIController>(UnityEngine.FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null) continue;
            if (player != null && e.gameObject == player) continue;
            Destroy(e.gameObject);
        }

        // Helper: safe destroy by predicate on GameObject
        void SafeDestroyIf(GameObject go, System.Func<GameObject, bool> predicate)
        {
            if (go == null) return;
            // never destroy player or this manager object
            if (player != null && go == player) return;
            if (go == this.gameObject) return;
            try
            {
                if (predicate(go))
                    Destroy(go);
            }
            catch { }
        }

        // Destroy by scanning scene objects - more robust for dynamically-named loot objects
        var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in allTransforms)
        {
            if (t == null || t.gameObject == null) continue;
            var go = t.gameObject;

            // skip root/system objects
            if (go == player || go == this.gameObject) continue;

            string name = go.name ?? "";

            // common name patterns for loot / dropped items / health / chests / exits
            bool nameMatches =
                name.StartsWith("Loot", System.StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("HealthPot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Health_Pot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Chest", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Pickup", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (nameMatches)
            {
                SafeDestroyIf(go, g => true);
                continue;
            }

            // inspect components for types that indicate loot/pickup objects
            var comps = go.GetComponents<Component>();
            bool hasLootComponent = false;
            if (comps != null)
            {
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    var tname = c.GetType().Name;
                    if (tname.IndexOf("Loot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tname.IndexOf("Pickup", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tname.IndexOf("HealthPot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasLootComponent = true;
                        break;
                    }
                }
            }

            if (hasLootComponent)
            {
                SafeDestroyIf(go, g => true);
                continue;
            }
        }

        // Also destroy any lingering containers named in your project (RoomManagers children etc.)
        var rm = GameObject.Find("RoomManagers");
        if (rm != null)
        {
            foreach (Transform child in rm.transform)
            {
                if (child == null) continue;
                // don't destroy player if it was parented here for some reason
                if (player != null && child.gameObject == player) continue;
                Destroy(child.gameObject);
            }
        }

        // Legacy: keep existing fallback attempts (try to remove by explicit type names for compatibility)
        void DestroyByTypeName(string typeName)
        {
            try
            {
                var monos = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var m in monos)
                {
                    if (m == null) continue;
                    if (m.GetType().Name == typeName)
                    {
                        if (m.gameObject != player && m.gameObject != this.gameObject)
                            Destroy(m.gameObject);
                    }
                }
            }
            catch { /* ignore */ }

            try
            {
                var type = Type.GetType(typeName) ?? typeof(UnityEngine.Object);
                var objs = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None);
                if (objs != null)
                {
                    foreach (var o in objs)
                    {
                        if (o == null) continue;
                        if (o is Component c)
                        {
                            if (c.gameObject != player && c.gameObject != this.gameObject)
                                Destroy(c.gameObject);
                        }
                        else if (o is GameObject go2)
                        {
                            if (go2 != player && go2 != this.gameObject) Destroy(go2);
                        }
                    }
                }
            }
            catch { /* ignore */ }
        }

        DestroyByTypeName("Loot");
        DestroyByTypeName("HealthPot");
        DestroyByTypeName("Chest");

        // Destroy any Exit GameObjects by name convention (Exit, Exit(Clone), etc.)
        var allT = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t2 in allT)
        {
            if (t2 == null || t2.gameObject == null) continue;
            var go = t2.gameObject;
            if (go == this.gameObject) continue;
            string n = go.name ?? "";
            if (n.StartsWith("Exit")) Destroy(go);
        }
    }

    // --- other helpers below (TryGetPlayerHealth, TrySetPlayerHealth, TryClearPlayerKey) ---
    // Keep the existing implementations you already have for those helpers.
    private int TryGetPlayerHealth(GameObject player)
    {
        if (player == null) return 0;

        // try dynamic GetComponent("PlayerController")
        try
        {
            var pcObj = player.GetComponent("PlayerController");
            if (pcObj != null)
            {
                var type = pcObj.GetType();
                var f = type.GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(pcObj);

                var p = type.GetProperty("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? type.GetProperty("Health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(pcObj);
            }
        }
        catch { /* ignore reflection errors */ }

        // fallback: typed PlayerController
        var typedPc = player.GetComponent<PlayerController>();
        if (typedPc != null)
        {
            try
            {
                var f = typedPc.GetType().GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) return (int)f.GetValue(typedPc);
            }
            catch { }
        }

        return 0;
    }

    private bool TrySetPlayerHealth(GameObject player, int value)
    {
        if (player == null) return false;

        try
        {
            var pcObj = player.GetComponent("PlayerController");
            if (pcObj != null)
            {
                var type = pcObj.GetType();
                var f = type.GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(int)) { f.SetValue(pcObj, value); return true; }

                var p = type.GetProperty("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? type.GetProperty("Health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(int)) { p.SetValue(pcObj, value); return true; }
            }
        }
        catch { /* ignore reflection errors */ }

        var typedPc = player.GetComponent<PlayerController>();
        if (typedPc != null)
        {
            var f = typedPc.GetType().GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                f.SetValue(typedPc, value);
                return true;
            }
        }

        return false;
    }

    private void TryClearPlayerKey(GameObject player)
    {
        if (player == null) return;

        var pk = player.GetComponent<PlayerKey>();
        if (pk != null) pk.HasExitKey = false;

        try
        {
            var pcObj = player.GetComponent("PlayerController");
            if (pcObj != null)
            {
                var type = pcObj.GetType();
                var f = type.GetField("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(bool)) { f.SetValue(pcObj, false); return; }

                var p = type.GetProperty("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? type.GetProperty("HasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(bool)) { p.SetValue(pcObj, false); return; }

                var m = type.GetMethod("RemoveExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? type.GetMethod("ClearExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null) m.Invoke(pcObj, null);
            }
        }
        catch { }
    }

    private void TryPlacePlayerAtSpawn(GameObject player)
    {
        if (player == null) return;

        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            var type = spawner.GetType();
            var method = type.GetMethod("PlacePlayerAtSpawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      ?? type.GetMethod("PlacePlayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      ?? type.GetMethod("SpawnPlayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                InvokeMethodWithOptionalGameObject(method, spawner, player);
                return;
            }
        }

        // fallback: try find object named "PlayerSpawnPoint" or "PlayerSpawn"
        GameObject spawn = GameObject.Find("PlayerSpawnPoint") ?? GameObject.Find("PlayerSpawn");
        if (spawn != null)
        {
            player.transform.position = spawn.transform.position;
            return;
        }

        // final fallback: try to find any GameObject tagged "PlayerSpawnPoint" but guard against missing tag
        try
        {
            var byTag = GameObject.FindWithTag("PlayerSpawnPoint");
            if (byTag != null)
            {
                player.transform.position = byTag.transform.position;
                return;
            }
        }
        catch { /* tag not defined -> ignore */ }

        Debug.Log("LevelManager: couldn't find PlayerSpawner/PlayerSpawnPoint - leaving player position unchanged.");
    }

    // helper used when calling spawner methods that may accept 0 or 1 parameter
    private void InvokeMethodWithOptionalGameObject(MethodInfo m, object instance, GameObject player)
    {
        if (m == null || instance == null) return;

        try
        {
            var parms = m.GetParameters();
            object[] args = null;

            if (parms.Length == 0)
            {
                args = null;
            }
            else if (parms.Length == 1)
            {
                var pType = parms[0].ParameterType;

                // if method expects a GameObject
                if (pType == typeof(GameObject))
                {
                    args = new object[] { player };
                }
                // if method expects a PlayerController (or another Component)
                else if (typeof(Component).IsAssignableFrom(pType))
                {
                    var comp = player != null ? player.GetComponent(pType) : null;
                    args = new object[] { comp };
                }
                // if it's a reference type, pass null fallback to let method handle it
                else if (!pType.IsValueType)
                {
                    args = new object[] { null };
                }
                else
                {
                    // unsupported parameter signature
                    Debug.LogWarning($"LevelManager: cannot invoke {m.Name} - unexpected parameter type {pType.Name}");
                    return;
                }
            }
            else
            {
                Debug.LogWarning($"LevelManager: cannot invoke {m.Name} - method requires {parms.Length} parameters");
                return;
            }

            m.Invoke(instance, args);
        }
        catch (TargetInvocationException tie)
        {
            Debug.LogWarning($"LevelManager: {m.Name} invocation threw: {tie.InnerException?.Message ?? tie.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"LevelManager: failed to invoke {m.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to call a generator in scene using common type / method names.
    /// Returns true if a method was invoked.
    /// </summary>
    private bool TryInvokeGenerator()
    {
        // candidate component type name substrings and method names to try
        string[] typeNames = new[] { "RoomsDungeonGenerator", "RandomDungeonGenerator", "DungeonGenerator", "RoomFirstDungeonGenerator", "AbstractDungeonGenerator" };
        // added "Regenerate" to cover generators that expose that method
        string[] methodNames = new[] { "Generate", "GenerateDungeon", "GenerateMap", "GenerateRooms", "GenerateLevel", "StartGeneration", "Regenerate" };

        var allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(UnityEngine.FindObjectsSortMode.None);
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;
            var t = mb.GetType();
            string tn = t.Name;
            if (!typeNames.Any(n => tn.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)) continue;

            // try methods
            foreach (var mName in methodNames)
            {
                var m = t.GetMethod(mName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (m != null)
                {
                    try
                    {
                        m.Invoke(mb, null);
                        Debug.Log($"LevelManager: invoked {t.Name}.{mName}()");
                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"LevelManager: failed to invoke {t.Name}.{mName}() -> {ex.Message}");
                    }
                }
            }
        }

        return false;
    }

    // somewhere after player spawn / when you want the "Next Level" banner to appear
    private void DisplayLevelUIForNewFloor()
    {
        // ensure any NextLevelUIController instance (even inactive) is activated then shown
        var nextUIControllers = Resources.FindObjectsOfTypeAll<NextLevelUIController>();
        var nextUI = nextUIControllers.FirstOrDefault();
        if (nextUI != null && !nextUI.gameObject.activeInHierarchy)
            nextUI.gameObject.SetActive(true);

        // now request it to show temporarily (this helper will auto-hide)
        NextLevelUIController.ShowTemporaryGlobal(2f);

        // ...existing code that continues level startup...
    }

    // Safe cleanup before starting a new run
    public static void PrepareForNewRun()
    {
        Debug.Log("=== LevelManager.PrepareForNewRun START ===");

        // 1) Find and destroy ALL player GameObjects (including clones)
        try
        {
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                
                // SKIP UIManager, HUDCanvas, and other persistent UI
                if (obj.name.Contains("UIManager") || 
                    obj.name.Contains("HUD") || 
                    obj.name.Contains("Canvas") ||
                    obj.GetComponent<Canvas>() != null)
                {
                    continue; // Don't destroy UI objects
                }
                
                // Check if it's a player by tag OR name
                if (obj.CompareTag("Player") || 
                    obj.name.Contains("PlayerCharacter") ||
                    obj.GetComponent<PlayerController>() != null)
                {
                    Debug.Log($"PrepareForNewRun: Destroying player object '{obj.name}'");
                    UnityEngine.Object.Destroy(obj);
                }
            }
        }
        catch (Exception ex) 
        { 
            Debug.LogWarning($"PrepareForNewRun player cleanup error: {ex.Message}"); 
        }

        // 2) Destroy all enemies
        try
        {
            var allMonos = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mb in allMonos)
            {
                if (mb == null || mb.gameObject == null) continue;
                
                // Skip UI components
                if (mb.GetComponent<Canvas>() != null || mb.name.Contains("UI")) continue;
                
                var typeName = mb.GetType().Name;
                
                if (typeName.Contains("AI") || typeName.Contains("Enemy"))
                {
                    Debug.Log($"PrepareForNewRun: Destroying enemy '{mb.gameObject.name}'");
                    UnityEngine.Object.Destroy(mb.gameObject);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PrepareForNewRun enemy cleanup error: {ex.Message}");
        }

        // 3) Reset health UI in inventory

        try
        {
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None
            );

            foreach (var p in players)
            {
                p.ResetHealthForNewRun();
                Debug.Log("PrepareForNewRun: Health reset on PlayerController");
            }
        }
        catch { }

        // 4) reset inventory items

        try
        {
            var inventoryController = FindFirstObjectByType<InventoryUIController>();
            if (inventoryController != null)
            {
                inventoryController.inventoryItems.Clear();  // clear all items
                inventoryController.RefreshUI();             // refresh UI to show empty slots
                Debug.Log("PrepareForNewRun: Inventory cleared.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PrepareForNewRun inventory reset error: {ex.Message}");
        }

        // 4) Reset singleton states without destroying them
        try
        {
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.ResetStats();
                Debug.Log("PrepareForNewRun: Reset PlayerStats");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PrepareForNewRun stats reset error: {ex.Message}");
        }

        // 4) Reset LevelManager state
        if (Instance != null)
        {
            Instance.currentFloor = 1;
            Instance.enemiesKilled = 0;
            Debug.Log("PrepareForNewRun: Reset LevelManager state");
        }

        Debug.Log("=== LevelManager.PrepareForNewRun END ===");
    }
}
