using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitKeyPickup : MonoBehaviour
{
    [Tooltip("Player tag")]
    public string playerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        GiveKeyToPlayer(other.gameObject);
        Debug.Log("ExitKeyPickup: Player picked up exit key.");
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
            if (f != null && f.FieldType == typeof(bool)) { f.SetValue(pc, true); return; }

            var p = type.GetProperty("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetProperty("HasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool)) { p.SetValue(pc, true); return; }

            var m = type.GetMethod("GiveExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetMethod("AddExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetMethod("PickupExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) m.Invoke(pc, null);
        }
        catch
        {
            // ignore reflection errors
        }
    }
}
