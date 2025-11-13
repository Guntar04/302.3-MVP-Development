using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Dash UI")]
    public Image dashIconOverlay;
    
    [Header("Player UI")]
    public Slider healthSlider;
    public GameObject shieldContainer;
    public ShieldUI shieldUI;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
