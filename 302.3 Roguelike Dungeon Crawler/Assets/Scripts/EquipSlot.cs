using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Slot Info")]
    public ItemType acceptedType;
    public Image itemIcon;
    public PlayerController playerController;
    public InventoryUIController inventoryController;

    private ItemData currentItem;
    private EquipmentStats currentItemStats;

    private void Awake()
    {
        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryUIController>();

        ClearSlot();
    }

    public bool AcceptItem(ItemData newItem, EquipmentStats stats, Loot.EquipmentType lootType)
    {
        if (newItem == null || stats == null) return false;

        ItemType convertedType = ConvertToItemType(lootType);
        if (convertedType != acceptedType) return false;

        if (currentItem != null) Unequip();

        currentItem = newItem;
        currentItemStats = stats;

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        playerController?.EquipItemStats(currentItemStats, lootType);

        UpdateIcon();
        return true;
    }

public void Unequip()
{
    if (currentItem == null) return;

    // Ensure inventoryController exists
    if (inventoryController == null)
        inventoryController = FindFirstObjectByType<InventoryUIController>();

    // Try to return item to inventory
    bool added = inventoryController != null && inventoryController.AddItem(currentItem);
    if (!added)
        Debug.LogWarning($"Could not unequip {currentItem.itemName}: Inventory full!");

    // Reset player stats
    if (playerController != null && currentItemStats != null)
    {
        Loot.EquipmentType lootType = ConvertToLootType(currentItem.itemType);
        playerController.UnequipItemStats(lootType);
    }

    // Clear slot
    currentItem = null;
    currentItemStats = null;
    UpdateIcon();
}


    public void OnPointerClick(PointerEventData eventData)
    {
        Unequip();
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
            default: return Loot.EquipmentType.Sword;
        }
    }
}
