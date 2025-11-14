using UnityEngine;
using UnityEngine.UI;

public class EquipSlot : MonoBehaviour
{
    [Header("Slot Info")]
    public ItemType acceptedType;              // Helmet, Chestplate, Weapon, Shield, etc.
    public Image itemIcon;                     // Icon for equipped item
    public PlayerController playerController;  // Reference to PlayerController
    public InventoryUIController inventoryController; // Reference to inventory to return items

    private ItemData currentItem;
    private EquipmentStats currentItemStats;

    private void Awake()
    {
        // Auto-find InventoryUIController if not assigned
        if (inventoryController == null)
            inventoryController = FindObjectOfType<InventoryUIController>();

        ClearSlot();
    }

    public bool AcceptItem(ItemData newItem, EquipmentStats stats, Loot.EquipmentType lootType)
    {
        if (newItem == null || stats == null)
        {
            Debug.LogWarning("EquipSlot: newItem or stats is null");
            return false;
        }

        ItemType convertedType = ConvertToItemType(lootType);
        if (convertedType != acceptedType)
        {
            Debug.LogWarning($"Cannot equip {newItem.itemName} in {acceptedType} slot (type={convertedType})");
            return false;
        }

        // Unequip old item if any
        if (currentItem != null)
            Unequip();

        currentItem = newItem;
        currentItemStats = stats;
        UpdateIcon();

        // Find playerController if null
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
            playerController.EquipItemStats(currentItemStats, lootType);
        else
            Debug.LogWarning("No PlayerController found! Could not equip stats.");

        Debug.Log($"Equipped {newItem.itemName} in {acceptedType} slot!");
        return true;
    }

    public void Unequip()
    {
        if (currentItem == null) return;

        // Return item to inventory
        if (inventoryController != null)
            inventoryController.AddItem(currentItem);

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

    public static Loot.EquipmentType ConvertToLootType(ItemType itemType)
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
        currentItemStats = null;

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
