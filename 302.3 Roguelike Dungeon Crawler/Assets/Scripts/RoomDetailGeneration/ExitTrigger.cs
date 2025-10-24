using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class ExitTrigger : MonoBehaviour
{
    public UnityEvent OnRequestNextFloor;

    [Header("No-Key UI")]
    public GameObject noKeyUIPrefab;
    public float noKeyUIDuration = 2f;

    private GameObject activeNoKeyUI;
    private Transform playerInRange;
    private bool playerIsInRange;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (!playerIsInRange || playerInRange == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            bool hasKey = PlayerHasExitKey(playerInRange.gameObject);
            if (hasKey)
            {
                Debug.Log("ExitTrigger: Next floor requested.");
                // invoke inspector hooks
                OnRequestNextFloor?.Invoke();

                // central level manager: performs the actual next-floor generation / transfer
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.GoToNextFloor(playerInRange.gameObject);
                }
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
    }

    private void ShowNoKeyUI(Transform player)
    {
        if (noKeyUIPrefab == null || player == null) return;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (activeNoKeyUI == null)
        {
            activeNoKeyUI = Instantiate(noKeyUIPrefab, canvas.transform);
            var popup = activeNoKeyUI.GetComponent<NoKeyPopup>();
            if (popup != null) popup.Initialize(player, canvas, noKeyUIDuration);
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

        var pk = player.GetComponent<PlayerKey>();
        if (pk != null) return pk.HasExitKey;

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
