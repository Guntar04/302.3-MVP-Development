using UnityEngine;
using UnityEngine.UI;

public class EquipSlot : MonoBehaviour
{
    [Header("Slot Info")]
    public ItemType acceptedType;        // Helmet, Chestplate, Weapon, Shield, etc.
    public Image itemIcon;               // Icon for equipped item
    public PlayerController playerController;      // Reference to PlayerController
    public InventoryManager inventoryManager;   // Reference to inventory to return items

    private ItemData currentItem;
    private EquipmentStats currentItemStats; // newss

    private void Awake()
    {
        ClearSlot();
    }

public bool AcceptItem(ItemData newItem, EquipmentStats stats, Loot.EquipmentType lootType)
{
    Debug.Log($"Trying to equip {newItem.itemName} in {acceptedType} slot (itemType={newItem.itemType})");

    if (newItem == null)
    {
        Debug.LogWarning("AcceptItem failed: newItem is null");
        return false;
    }

    if (stats == null)
    {
        Debug.LogWarning($"AcceptItem failed: stats is null for {newItem.itemName}");
        return false;
    }


    ItemType convertedType = ConvertToItemType(lootType);

    if (convertedType != acceptedType)
    {
        Debug.LogWarning($"{newItem.itemName} type ({convertedType}) does not match slot type ({acceptedType})");
        return false;
    }

    // Unequip old item if any
    if (currentItem != null) Unequip();

    currentItem = newItem;
    currentItemStats = stats;
    UpdateIcon();

    if (playerController != null)
    {
        playerController.EquipItemStats(currentItemStats, lootType);
        Debug.Log($"Equipped {newItem.itemName} in {acceptedType} slot!");
    }

    return true;
}

public void Unequip()
{
    if (currentItem == null) return;

    if (inventoryManager != null)
        inventoryManager.AddItem(currentItem);

    if (playerController != null && currentItemStats != null)
    {
        Loot.EquipmentType lootType = ConvertToLootType(currentItem.itemType);
        playerController.UnequipItemStats(lootType);
    }

    currentItem = null;
    currentItemStats = null;
    UpdateIcon();
}


public ItemType ConvertToItemType(Loot.EquipmentType lootType)
{
    switch (lootType)
    {
        case Loot.EquipmentType.Sword: return ItemType.Weapon;
        case Loot.EquipmentType.Chestplate: return ItemType.Chestplate;
        case Loot.EquipmentType.Helmet: return ItemType.Helmet;
        case Loot.EquipmentType.Pants: return ItemType.Pants;
        case Loot.EquipmentType.Boots: return ItemType.Boots;
        case Loot.EquipmentType.Shield: return ItemType.Shield;
        default: return ItemType.Consumable;
    }
}

public Loot.EquipmentType ConvertToLootType(ItemType itemType)
{
    switch (itemType)
    {
        case ItemType.Weapon: return Loot.EquipmentType.Sword;
        case ItemType.Chestplate: return Loot.EquipmentType.Chestplate;
        case ItemType.Helmet: return Loot.EquipmentType.Helmet;
        case ItemType.Pants: return Loot.EquipmentType.Pants;
        case ItemType.Boots: return Loot.EquipmentType.Boots;
        case ItemType.Shield: return Loot.EquipmentType.Shield;
        default: return Loot.EquipmentType.Sword; // fallback
    }
}

    private void ClearSlot()
    {
        currentItem = null;
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
    }

    private void UpdateIcon()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = currentItem != null ? currentItem.icon : null;
            itemIcon.enabled = currentItem != null;
        }
    }

    public ItemData GetEquippedItem() => currentItem;

    public void OnClickSlot()
    {
        if (currentItem != null)
            Unequip();
    }
}
