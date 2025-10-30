using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class PlayerStats : MonoBehaviour
{
    public static event Action OnStatsChanged;
    public List<ItemData> equippedItems = new List<ItemData>();

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
    public ItemData equippedShield;


private void Start()
{
    RecalculateStats();
    currentHealth = GetMaxHealth();
    currentShield = GetMaxShield();
    StartCoroutine(NotifyAfterStart());
}

private IEnumerator NotifyAfterStart()
{
    yield return null; // wait one frame
    NotifyStatsChanged();
}

    public void EquipItem(ItemData item)
{
    if (item == null) return;

    switch (item.itemType)
    {
        case ItemType.Helmet:
            equippedHelmet = item;
            break;
        case ItemType.Armor:
            equippedArmor = item;
            break;
        case ItemType.Shield:
            equippedShield = item; // or create a separate equippedShield variable if you prefer
            break;
        case ItemType.Weapon:
            equippedWeapon = item;
            break;
    }

        equippedItems.RemoveAll(i => i.itemType == item.itemType);

    // Add new one
    equippedItems.Add(item);

    RecalculateStats();

    // Fill up to the new max
    currentHealth = GetMaxHealth();
    currentShield = GetMaxShield();

  Debug.Log($"[EquipItem] Equipped {item.itemName} | New MaxShield: {GetMaxShield()} | CurrentShield: {currentShield}");

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
    int totalShield = baseShield;

    // Loop through equipped items
    foreach (var item in equippedItems)
    {
        if (item == null) continue;

        if (item.itemType == ItemType.Armor || item.itemType == ItemType.Shield)
        {
            totalShield += item.shieldBonus;
        }
    }

    currentShield = totalShield;
    Debug.Log("[RecalculateStats] Total shield recalculated: " + totalShield);

    NotifyStatsChanged();
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
    if (equippedShield != null) max += equippedShield.healthBonus;
    if (equippedWeapon != null) max += equippedWeapon.healthBonus;
    return max;
}

public int GetMaxShield()
{
    int max = baseShield;
    if (equippedHelmet != null) max += equippedHelmet.shieldBonus;
    if (equippedArmor != null) max += equippedArmor.shieldBonus;
    if (equippedShield != null) max += equippedShield.shieldBonus;
    if (equippedWeapon != null) max += equippedWeapon.shieldBonus;
     Debug.Log($"[GetMaxShield] Total shield: {max}");
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

