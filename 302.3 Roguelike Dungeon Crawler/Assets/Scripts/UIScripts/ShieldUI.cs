using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShieldUI : MonoBehaviour
{
    [Header("Shield UI Settings")]
    public Image[] shieldIcons; // Array of shield icons
    public Sprite filledSprite; // Sprite for filled shields
    public Sprite emptySprite; // Sprite for empty shields

    public Shield playerShield;

    /// <summary>
    /// Binds the Shield component to the UI.
    /// </summary>
    public void BindShield(Shield shield)
    {
        if (shield == null) return;

        // Unsubscribe from previous shield events
        if (playerShield != null)
        {
            playerShield.OnShieldChanged -= UpdateShieldUI;
        }

        // Bind to the new shield
        playerShield = shield;
        playerShield.OnShieldChanged += UpdateShieldUI;

        // Update the UI immediately
        UpdateShieldUI(playerShield.CurrentShields);
    }

    /// <summary>
    /// Updates the shield UI based on the current shield count.
    /// </summary>
    private void UpdateShieldUI(int currentShields)
    {
        for (int i = 0; i < shieldIcons.Length; i++)
        {
            shieldIcons[i].sprite = i < currentShields ? filledSprite : emptySprite;
        }
    }

    private void OnDisable()
    {
        if (playerShield != null)
        {
            playerShield.OnShieldChanged -= UpdateShieldUI;
        }
    }
}
