using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ExitKeyUI : MonoBehaviour
{
    public Image keyImage; // assign in inspector (will use GetComponent<Image>() if empty)
    public Color hasKeyColor = Color.white;
    public Color noKeyColor = Color.black;
    public string playerTag = "Player";

    GameObject player;
    PlayerKey pk;
    bool lastHasKey = false;

    void Awake()
    {
        if (keyImage == null) keyImage = GetComponent<Image>();
        ApplyColor(false);
    }

    void Update()
    {
        if (player == null)
            player = GameObject.FindWithTag(playerTag);

        if (player == null) return;

        // prefer PlayerKey component
        pk = pk ?? player.GetComponent<PlayerKey>();

        bool hasKey = false;
        if (pk != null) hasKey = pk.HasExitKey;
        else
        {
            // fallback: reflect into PlayerController if present
            var pc = player.GetComponent("PlayerController");
            if (pc != null)
                hasKey = QueryPlayerControllerHasKey(pc);
        }

        if (hasKey != lastHasKey)
        {
            lastHasKey = hasKey;
            ApplyColor(hasKey);
        }
    }

    bool QueryPlayerControllerHasKey(object pc)
    {
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

    void ApplyColor(bool hasKey)
    {
        if (keyImage == null) return;
        keyImage.color = hasKey ? hasKeyColor : noKeyColor;
    }
}
