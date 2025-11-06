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

    public static Loot.EquipmentType ConvertToLootType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                return Loot.EquipmentType.Sword;
            case ItemType.Chestplate:
                return Loot.EquipmentType.Armour;
            default:
                return Loot.EquipmentType.Sword; // fallback default
        }
    }



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

    // convert Loot.EquipmentType to ItemType for slot checking
    ItemType convertedType = lootType == Loot.EquipmentType.Sword ? ItemType.Weapon : ItemType.Chestplate;

    if (convertedType != acceptedType)
    {
        Debug.LogWarning($"{newItem.itemName} type ({convertedType}) does not match slot type ({acceptedType})");
        return false;
    }

    // unequip and equip logic...
    if (currentItem != null) Unequip();

    currentItem = newItem;
    currentItemStats = stats;
    UpdateIcon();

    if (playerController != null)
        playerController.EquipItemStats(currentItemStats, lootType);

    Debug.Log($"Equipped {newItem.itemName} in {acceptedType} slot!");
    return true;
}


    public void Unequip()
    {
        if (currentItem == null) return;

        // Return to inventory
        if (inventoryManager != null)
            inventoryManager.AddItem(currentItem);

        // Remove stats from player
        if (playerController != null && currentItemStats != null)
            playerController.UnequipItemStats(
                currentItem.itemType == ItemType.Weapon ? Loot.EquipmentType.Sword : Loot.EquipmentType.Armour
            );

        currentItem = null;
        currentItemStats = null;
        UpdateIcon();
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
