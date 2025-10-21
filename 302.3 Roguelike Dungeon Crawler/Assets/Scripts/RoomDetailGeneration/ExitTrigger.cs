using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class ExitTrigger : MonoBehaviour
{
    public UnityEvent OnRequestNextFloor;

    [Header("No-Key UI")]
    public GameObject noKeyUIPrefab;      // assign your UI prefab (TextMeshProUGUI inside a RectTransform)
    public float noKeyUIDuration = 2f;

    // runtime instance (one per trigger)
    private GameObject activeNoKeyUI;

    // runtime player reference & in-range flag (more reliable than relying on OnTriggerStay)
    private Transform playerInRange;
    private bool playerIsInRange;

    private void Awake()
    {
        // ensure collider is a trigger at runtime
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        // check input centrally each frame while player is in-range
        if (!playerIsInRange || playerInRange == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            bool hasKey = PlayerHasExitKey(playerInRange.gameObject);
            if (hasKey)
            {
                Debug.Log("ExitTrigger: Next floor requested.");
                OnRequestNextFloor?.Invoke();
            }
            else
            {
                Debug.Log("ExitTrigger: Player needs the exit key to proceed.");
                ShowNoKeyUI(playerInRange);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = other.transform;
        playerIsInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (other.transform == playerInRange)
        {
            playerInRange = null;
            playerIsInRange = false;
        }

        // DO NOT destroy the activeNoKeyUI here — let it live out its lifetime so it follows player for the configured duration.
        // If you want the popup to dismiss immediately when leaving, uncomment the lines below.
        /*
        if (activeNoKeyUI != null)
        {
            Destroy(activeNoKeyUI);
            activeNoKeyUI = null;
        }
        */
    }

    private void ShowNoKeyUI(Transform player)
    {
        if (noKeyUIPrefab == null || player == null) return;

        // find a canvas in the scene
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (activeNoKeyUI == null)
        {
            activeNoKeyUI = Instantiate(noKeyUIPrefab, canvas.transform);
            var popup = activeNoKeyUI.GetComponent<NoKeyPopup>();
            if (popup != null) popup.Initialize(player, canvas, noKeyUIDuration);
            else
            {
                // fallback: ensure instantiated rect transform is positioned above player immediately
                var rt = activeNoKeyUI.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 screenPoint = Camera.main != null ? Camera.main.WorldToScreenPoint(player.position) : Vector2.zero;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(), screenPoint + Vector2.up * 60f, canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null, out var anchored);
                    rt.anchoredPosition = anchored;
                }
            }
        }
        else
        {
            var popup = activeNoKeyUI.GetComponent<NoKeyPopup>();
            if (popup != null) popup.Refresh(noKeyUIDuration);
        }
    }

    private bool PlayerHasExitKey(GameObject player)
    {
        if (player == null) return false;

        // 1) check PlayerKey component
        var pk = player.GetComponent<PlayerKey>();
        if (pk != null) return pk.HasExitKey;

        // 2) try PlayerController fields/properties/methods via reflection
        var pc = player.GetComponent("PlayerController");
        if (pc == null) return false;

        var type = pc.GetType();
        try
        {
            var f = type.GetField("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(pc);

            var p = type.GetProperty("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetProperty("HasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(pc);

            var m = type.GetMethod("HasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetMethod("HasKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null && m.ReturnType == typeof(bool)) return (bool)m.Invoke(pc, null);
        }
        catch { }

        return false;
    }
}
