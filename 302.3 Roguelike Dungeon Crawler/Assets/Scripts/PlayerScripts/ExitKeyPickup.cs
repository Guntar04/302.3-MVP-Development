using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitKeyPickup : MonoBehaviour
{
    [Tooltip("Player tag")]
    public string playerTag = "Player";

    [Tooltip("UI prefab to show when no key is acquired")]
    public GameObject noKeyUIPrefab;

    [Tooltip("Duration to show the no key UI")]
    public float noKeyUIDuration = 2f;

    private bool hasTriggered = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return; // Prevent multiple triggers
        hasTriggered = true;

        if (!other.CompareTag(playerTag)) return;

        // Show the key acquired UI above the player
        ShowKeyAcquiredUI(other.transform);

        // Give the key to the player
        GiveKeyToPlayer(other.gameObject);

        Debug.Log("ExitKeyPickup: Player picked up exit key.");

        // Destroy the ExitKeyPickup GameObject
        Destroy(gameObject);
    }

    private void GiveKeyToPlayer(GameObject player)
    {
        if (player == null) return;

        // Prefer PlayerKey component
        var pk = player.GetComponent<PlayerKey>();
        if (pk == null) pk = player.AddComponent<PlayerKey>();
        pk.HasExitKey = true;

        // Also attempt to set PlayerController variables if present (for compatibility)
        var pc = player.GetComponent("PlayerController");
        if (pc == null) return;

        var type = pc.GetType();
        try
        {
            var f = type.GetField("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool)) { f.SetValue(pc, true); }

            var p = type.GetProperty("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetProperty("HasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool)) { p.SetValue(pc, true); }

            var m = type.GetMethod("GiveExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetMethod("AddExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetMethod("PickupExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) m.Invoke(pc, null);
        }
        catch
        {
            // ignore reflection errors
        }

        // Show the key acquired UI
        ShowKeyAcquiredUI(player.transform);
    }

    private void ShowKeyAcquiredUI(Transform player)
    {
        if (noKeyUIPrefab == null || player == null) return;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var keyUIInstance = Instantiate(noKeyUIPrefab, canvas.transform);
        var rectTransform = keyUIInstance.GetComponent<RectTransform>();

        // Convert player's position to screen space and offset it above the player
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(player.position + new Vector3(0, 1, 0));
        rectTransform.position = screenPosition;

        Destroy(keyUIInstance, noKeyUIDuration); // Automatically destroy the UI after the duration
    }
}
