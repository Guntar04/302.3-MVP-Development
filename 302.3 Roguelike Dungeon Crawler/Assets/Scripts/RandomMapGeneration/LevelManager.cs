using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro; // Required for TextMeshPro
using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public int currentFloor = 1;

    [Header("Player spawn waiting")]
    public float playerSpawnWaitTime = 1.0f;

    [Header("Events")]
    public UnityEvent<int> OnBeforeFloorUnload;
    public UnityEvent<int> OnAfterFloorLoad;

    [Header("Level UI")]
    public GameObject nextLevelUI; // Reference to the NextLevelUI GameObject
    public TextMeshProUGUI levelText; // Reference to the TextMeshPro component

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(DisplayLevelUI(currentFloor));
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

        // Save player state early
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

        // Display the level UI
        StartCoroutine(DisplayLevelUI(currentFloor));

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
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) newPlayer = pc.gameObject;
            }

            if (newPlayer != null) break;

            waited += Time.deltaTime;
            yield return null;
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
                        var pc = FindFirstObjectByType<PlayerController>();
                        if (pc != null) newPlayer = pc.gameObject;
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
            bool restored = TrySetPlayerHealth(newPlayer, savedHealth);
            if (restored) Debug.Log($"LevelManager: restored player health = {savedHealth}");
        }

        // clear key safety
        TryClearPlayerKey(newPlayer ?? player);

        // position player at spawn (safe routine)
        TryPlacePlayerAtSpawn(newPlayer ?? player);

        yield return null;

        OnAfterFloorLoad?.Invoke(currentFloor);
        Debug.Log($"LevelManager: floor {currentFloor} ready.");
    }

    private IEnumerator DisplayLevelUI(int level)
    {
        if (nextLevelUI != null && levelText != null)
        {
            // Update the level text
            levelText.text = $"{level:D2}";

            // Show the UI
            nextLevelUI.SetActive(true);

            // Wait for 5 seconds
            yield return new WaitForSeconds(5f);

            // Hide the UI
            nextLevelUI.SetActive(false);
        }
        else
        {
            Debug.LogWarning("LevelManager: NextLevelUI or LevelText is not assigned.");
        }
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

        // Helper to destroy by runtime type name / type object safely
        void DestroyByTypeName(string typeName)
        {
            // try to find as Component first (fast, typed)
            try
            {
                var monos = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(UnityEngine.FindObjectsSortMode.None);
                foreach (var m in monos)
                {
                    if (m == null) continue;
                    if (m.GetType().Name == typeName) Destroy(m.gameObject);
                }
            }
            catch { /* ignore */ }

            // fallback: use reflection FindObjectsByType(typeof(T)) which returns UnityEngine.Object[]
            try
            {
                var type = Type.GetType(typeName) ?? typeof(UnityEngine.Object);
                var objs = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None);
                if (objs != null)
                {
                    foreach (var o in objs)
                    {
                        if (o == null) continue;
                        if (o is Component c) Destroy(c.gameObject);
                        else if (o is GameObject go) Destroy(go);
                    }
                }
            }
            catch { /* ignore */ }
        }

        // destroy loot items, health pots and chests by name/type
        DestroyByTypeName("Loot");
        DestroyByTypeName("HealthPot");
        DestroyByTypeName("Chest");

        // Destroy any Exit GameObjects by name convention (Exit, Exit(Clone), etc.)
        var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in allTransforms)
        {
            if (t == null || t.gameObject == null) continue;
            var go = t.gameObject;
            if (go == this.gameObject) continue;
            string n = go.name ?? "";
            if (n.StartsWith("Exit")) Destroy(go);
        }

        // If you use container GameObjects (e.g. RoomManagers -> SpawnedEnemies/Chests), clear their children
        var rm = GameObject.Find("RoomManagers");
        if (rm != null)
        {
            foreach (Transform child in rm.transform)
            {
                if (child == null) continue;
                Destroy(child.gameObject);
            }
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

        var allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
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
}
