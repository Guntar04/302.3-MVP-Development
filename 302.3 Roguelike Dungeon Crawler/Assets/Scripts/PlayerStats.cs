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
        currentHealth = baseHealth;
        currentShield = baseShield;
        UpdateStats();
    }

    public void EquipItem(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Helmet:
                equippedHelmet = item;
                break;
            case ItemType.Armor:
                equippedArmor = item;
                break;
            case ItemType.Weapon:
                equippedWeapon = item;
                break;
        }
        UpdateStats();
    }

    void UpdateStats()
    {
        int totalShield = baseShield;
        if (equippedHelmet != null) totalShield += equippedHelmet.shieldBonus;
        if (equippedArmor != null) totalShield += equippedArmor.shieldBonus;

        currentShield = totalShield;
        OnStatsChanged?.Invoke();
    }
}
