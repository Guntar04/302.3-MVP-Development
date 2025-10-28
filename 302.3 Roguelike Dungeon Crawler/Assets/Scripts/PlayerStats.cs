using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static event Action OnStatsChanged;

    [Header("Base Stats")]
    public int baseHealth = 100;
    public int baseShield = 0;

    [Header("Current Stats")]
    public int currentHealth;
    public int currentShield;

    [Header("Equipped Items")]
    public ItemData equippedHelmet;
    public ItemData equippedArmor;
    public ItemData equippedWeapon;

    private void Start()
    {
        // Initialize with max values on start
        RecalculateStats();
        currentHealth = GetMaxHealth();
        currentShield = GetMaxShield();
        NotifyStatsChanged();
    }

    public void EquipItem(ItemData item)
    {
        if (item == null) return;

        switch (item.itemType)
        {
            case ItemType.Helmet: equippedHelmet = item; break;
            case ItemType.Armor: equippedArmor = item; break;
            case ItemType.Weapon: equippedWeapon = item; break;
        }

        RecalculateStats();

        // Ensure shield/health are topped up to new max if needed
        currentHealth = Mathf.Min(currentHealth, GetMaxHealth());
        currentShield = Mathf.Min(currentShield, GetMaxShield());

        NotifyStatsChanged();
    }

    public void UnequipItem(ItemData item)
    {
        if (item == null) return;

        switch (item.itemType)
        {
            case ItemType.Helmet: if (equippedHelmet == item) equippedHelmet = null; break;
            case ItemType.Armor: if (equippedArmor == item) equippedArmor = null; break;
            case ItemType.Weapon: if (equippedWeapon == item) equippedWeapon = null; break;
        }

        RecalculateStats();
        NotifyStatsChanged();
    }

    public void RecalculateStats()
    {
        int totalHealth = GetMaxHealth();
        int totalShield = GetMaxShield();

        // Clamp current values so they never exceed the new max
        currentHealth = Mathf.Clamp(currentHealth, 0, totalHealth);
        currentShield = Mathf.Clamp(currentShield, 0, totalShield);
    }

    public void TakeDamage(int damage)
    {
        int remaining = damage;

        if (currentShield > 0)
        {
            if (currentShield >= remaining)
            {
                currentShield -= remaining;
                remaining = 0;
            }
            else
            {
                remaining -= currentShield;
                currentShield = 0;
            }
        }

        if (remaining > 0)
            currentHealth = Mathf.Max(currentHealth - remaining, 0);

        NotifyStatsChanged();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, GetMaxHealth());
        NotifyStatsChanged();
    }

    public int GetMaxHealth()
    {
        int max = baseHealth;
        if (equippedHelmet != null) max += equippedHelmet.healthBonus;
        if (equippedArmor != null) max += equippedArmor.healthBonus;
        if (equippedWeapon != null) max += equippedWeapon.healthBonus;
        return max;
    }

    public int GetMaxShield()
    {
        int max = baseShield;
        if (equippedHelmet != null) max += equippedHelmet.shieldBonus;
        if (equippedArmor != null) max += equippedArmor.shieldBonus;
        if (equippedWeapon != null) max += equippedWeapon.shieldBonus;
        return max;
    }

    public void AddShield(int amount)
    {
        currentShield = Mathf.Min(currentShield + amount, GetMaxShield());
        NotifyStatsChanged();
    }

    public void RemoveShield(int amount)
    {
        currentShield = Mathf.Max(currentShield - amount, 0);
        NotifyStatsChanged();
    }

    public void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
}
