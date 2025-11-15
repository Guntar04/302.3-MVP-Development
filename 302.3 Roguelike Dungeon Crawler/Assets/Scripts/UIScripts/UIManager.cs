using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // <--- added

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Dash UI")]
    public Image dashIconOverlay;
    
    [Header("Player UI")]
    public Slider healthSlider;
    public GameObject shieldContainer;
    public ShieldUI shieldUI;

    [Header("Scene Persistence")]
    [Tooltip("List of scene names where the HUD should persist. If the loaded scene is NOT in this list the UIManager will self-destruct.")]
    public string[] keepOnScenes = new string[] { "SampleScene" }; // set your main gameplay scene name(s) here

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // subscribe to scene loaded so we can destroy the persistent HUD when leaving gameplay scenes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // if keepOnScenes contains the loaded scene, keep the HUD, otherwise destroy it
        if (keepOnScenes != null && keepOnScenes.Length > 0)
        {
            bool keep = false;
            foreach (var s in keepOnScenes)
            {
                if (string.Equals(s, scene.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    keep = true;
                    break;
                }
            }
            if (!keep)
            {
                Debug.Log($"UIManager: scene '{scene.name}' not in keepOnScenes -> destroying HUD");
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
                Destroy(gameObject);
            }
        }
    }

    // Call after a new player is spawned so UI rebinds to the new player instance
    // Public entrypoint — run binding a frame later to avoid initialization order races
    public void BindPlayer(GameObject player)
    {
        if (player == null) return;
        // start coroutine on this UI manager so binding occurs after a frame (lets player Awake/Start run first)
        StartCoroutine(BindPlayerRoutine(player));
    }

    private IEnumerator BindPlayerRoutine(GameObject player)
    {
        // wait two frames to reduce ordering races
        yield return null;
        yield return null;

        if (player == null) yield break;

        // Try to find Shield component on the actual player instance (prefer GetComponentInChildren on the player)
        var playerShield = player.GetComponentInChildren<Shield>(true);
        if (playerShield != null)
        {
            Debug.Log($"UIManager: binding Shield from player '{player.name}' instanceID {playerShield.GetInstanceID()} CurrentShields={playerShield.CurrentShields}");
            if (shieldUI != null)
            {
                shieldUI.BindShield(playerShield);
            }
            else
            {
                Debug.LogWarning("UIManager: shieldUI reference is null; cannot bind shield UI.");
            }
            yield break;
        }

        // fallback: try to find any Shield in the scene (include inactive objects)
        var anyShield = FindFirstObjectByType<Shield>(UnityEngine.FindObjectsInactive.Include);
        if (anyShield != null)
        {
            Debug.LogWarning($"UIManager: player had no Shield component; falling back to first found Shield instanceID {anyShield.GetInstanceID()}");
            shieldUI?.BindShield(anyShield);
            yield break;
        }

        Debug.LogWarning("UIManager: no Shield component found to bind for player.");
    }
}
