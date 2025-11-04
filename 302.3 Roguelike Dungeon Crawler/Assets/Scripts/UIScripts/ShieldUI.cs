using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI that displays a row of shield icons.
/// - Assign the player Shield (or the script will try to find PlayerController -> Shield).
/// - Assign shieldIcons list (Image components), order left-to-right (icon 0 = first shield).
/// - Optionally set filled/empty sprites; if null the Image.enabled approach is used.
/// </summary>
public class ShieldUI : MonoBehaviour
{
    [Tooltip("Drag the player's Shield component here (optional). If empty, will attempt to find a PlayerController in scene and get Shield.")]
    public Shield playerShield;

    [Tooltip("UI Image slots representing shields (ordered).")]
    public List<Image> shieldIcons = new List<Image>();

    [Tooltip("Optional sprites to show filled vs empty. If left empty, widget will enable/disable images.")]
    public Sprite filledSprite;
    public Sprite emptySprite;

    private void OnDestroy()
    {
        if (playerShield != null)
            playerShield.OnShieldChanged -= OnShieldChanged;
    }

    // replace Start() with this coroutine-based Start so we can wait for a spawned player
    private IEnumerator Start()
    {
        // if inspector-assigned is a prefab/asset, ignore it
        if (playerShield != null)
        {
            var go = playerShield.gameObject;
            if (!go.scene.IsValid()) playerShield = null;
        }

        // try to find runtime shield for up to 1 second (safe when player is spawned shortly after)
        float timeout = 1.0f;
        float waited = 0f;
        while (playerShield == null && waited < timeout)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerShield = pc.GetComponent<Shield>();

            if (playerShield == null)
            {
                try
                {
                    var tagged = GameObject.FindWithTag("Player");
                    if (tagged != null) playerShield = tagged.GetComponent<Shield>();
                }
                catch { /* ignore missing tag */ }
            }

            if (playerShield != null) break;

            yield return null;
            waited += Time.deltaTime;
        }

        if (playerShield == null)
        {
            Debug.LogWarning("ShieldUI: no runtime Shield found. Leave Player Shield empty in inspector to auto-find.");
            UpdateIcons(0);
            yield break;
        }

        // subscribe and initialize UI
        playerShield.OnShieldChanged += OnShieldChanged;
        OnShieldChanged(playerShield.CurrentShields);
    }

    private void OnShieldChanged(int newCount) => UpdateIcons(newCount);

    private void UpdateIcons(int newCount)
    {
        for (int i = 0; i < shieldIcons.Count; i++)
        {
            var img = shieldIcons[i];
            if (img == null) continue;

            if (filledSprite != null && emptySprite != null)
            {
                img.sprite = (i < newCount) ? filledSprite : emptySprite;
                img.enabled = true;
            }
            else
            {
                // if no sprites provided, use enabled state
                img.enabled = (i < newCount);
            }
        }
    }
}
