using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerStatsInventoryDisplay : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI shieldText;

    private PlayerController player;
    private Shield shield;

    IEnumerator Start()
    {
        // Wait until a PlayerController exists in the scene
        while (FindFirstObjectByType<PlayerController>() == null)
        {
            yield return null;
        }

        player = FindFirstObjectByType<PlayerController>();

        // Get the Shield component (if it exists)
        shield = player.GetComponent<Shield>();
        if (shield != null && shieldText != null)
        {
            // Subscribe to shield changes
            shield.OnShieldChanged += UpdateShieldUI;
            UpdateShieldUI(shield.CurrentShields);
        }

        // Initialize health text immediately
        if (healthText != null)
            UpdateHealthUI();
    }

    void Update()
    {
        // Update health every frame
        if (player != null && healthText != null)
            UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        healthText.text = player.health.ToString();
    }

    private void UpdateShieldUI(int value)
    {
        if (shieldText != null)
            shieldText.text = value.ToString();
    }

    private void OnDestroy()
    {
        // Clean up subscription
        if (shield != null)
            shield.OnShieldChanged -= UpdateShieldUI;
    }
}
