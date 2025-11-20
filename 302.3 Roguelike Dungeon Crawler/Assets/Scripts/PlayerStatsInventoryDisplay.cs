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
    while (true)
    {
        var newPlayer = FindFirstObjectByType<PlayerController>();
        
        if (newPlayer != null && newPlayer != player)
        {
            ConnectToPlayer(newPlayer);
        }

        yield return new WaitForSeconds(0.2f);
    }
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

    private void ConnectToPlayer(PlayerController newPlayer)
{
    player = newPlayer;

    // Disconnect old shield listener
    if (shield != null)
        shield.OnShieldChanged -= UpdateShieldUI;

    // Connect new shield
    shield = player.GetComponent<Shield>();
    if (shield != null)
    {
        shield.OnShieldChanged += UpdateShieldUI;
        UpdateShieldUI(shield.CurrentShields);
    }

    // Update health immediately
    UpdateHealthUI();
}


    private void OnDestroy()
    {
        // Clean up subscription
        if (shield != null)
            shield.OnShieldChanged -= UpdateShieldUI;
    }
}
