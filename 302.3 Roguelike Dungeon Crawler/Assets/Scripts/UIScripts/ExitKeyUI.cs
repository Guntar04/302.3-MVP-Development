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
        // ensure we have an Image reference
        if (keyImage == null) keyImage = GetComponent<Image>();
        ApplyColor(false);
    }

    void Update()
    {
        // try to find the player once
        if (player == null) player = GameObject.FindWithTag(playerTag);
        if (player == null) return;

        // Prefer PlayerKey component
        var playerKeyComp = player.GetComponent<PlayerKey>();
        bool hasKey = false;
        if (playerKeyComp != null)
        {
            hasKey = playerKeyComp.HasExitKey;
        }
        else
        {
            // Fallback: reflect into PlayerController-like component
            var pc = player.GetComponent("PlayerController");
            if (pc != null)
            {
                var t = pc.GetType();
                try
                {
                    var f = t.GetField("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null && f.FieldType == typeof(bool)) hasKey = (bool)f.GetValue(pc);
                    else
                    {
                        var p = t.GetProperty("HasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                ?? t.GetProperty("hasExitKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (p != null && p.PropertyType == typeof(bool)) hasKey = (bool)p.GetValue(pc);
                    }
                }
                catch { /* ignore reflection errors */ }
            }
        }

        // Only update visuals if changed
        if (hasKey != lastHasKey)
        {
            lastHasKey = hasKey;
            ApplyColor(hasKey);
        }
    }

    void ApplyColor(bool hasKey)
    {
        if (keyImage == null) return;
        keyImage.color = hasKey ? hasKeyColor : noKeyColor;
    }

    // Public helper so other code can explicitly tell the UI the player has acquired/removed key
    public void SetHasKey(bool hasKey)
    {
        if (keyImage == null) return;
        if (hasKey != lastHasKey)
        {
            lastHasKey = hasKey;
            ApplyColor(hasKey);
        }
        Debug.Log($"ExitKeyUI.SetHasKey -> hasKey={hasKey}");
    }
}
