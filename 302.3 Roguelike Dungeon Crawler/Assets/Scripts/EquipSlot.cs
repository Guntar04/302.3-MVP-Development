using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public class EquipSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Slot Info")]
    public ItemType acceptedType;
    public Image itemIcon;
    public PlayerController playerController;
    public InventoryUIController inventoryController;

    private ItemData currentItem;
    private EquipmentStats currentItemStats;

    // NEW: only allow real clicks after UI ready
    private bool allowClick = true; // default true, set false during scene rebuild if needed

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

        if (inventoryController != null)
        {
            int index = inventoryController.inventoryItems.IndexOf(newItem);
            if (index >= 0)
                inventoryController.RemoveItem(index);
        }

            currentItem = newItem;
            currentItemStats = stats;

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        playerController?.EquipItemStats(currentItemStats, lootType);

        UpdateIcon();
        Debug.Log($"[EquipSlot] Equipped {currentItem.itemName} in {acceptedType} slot.");
        return true;
    }

    public void EquipFromInventory(int slotIndex)
{
    if (slotIndex < 0 || slotIndex >= inventoryController.inventoryItems.Count) return;

    ItemData item = inventoryController.inventoryItems[slotIndex];
    EquipmentStats equipmentStats = item.equipmentStats; // or wherever your stats are stored

    if (AcceptItem(item, equipmentStats, ConvertToLootType(item.itemType)))
    {
        // remove the exact item from inventory
        inventoryController.RemoveItem(slotIndex);
    }
}


    public void Unequip()
    {
        
        playerController = FindObjectOfType<PlayerController>();

        if (!allowClick) return;
        if (currentItem == null) return;

        ItemData itemToReturn = currentItem;

        Debug.Log($"[EquipSlot] Unequipping {currentItem.itemName} from {acceptedType} slot.");

        Debug.Log($"[EquipSlot] allowClick={allowClick}, currentItem={currentItem?.itemName}");

        // Ensure inventoryController exists
        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryUIController>();

        

        bool added = inventoryController.AddItem(itemToReturn, allowDuplicate: false); // always adds new reference
        Debug.Log($"Inventory after adding back: {string.Join(", ", inventoryController?.GetInventoryNames() ?? new string[0])}");
        Debug.Log($"Slot after clearing: {currentItem?.itemName ?? "empty"}");

        if (!added)
            Debug.LogWarning($"Could not unequip {currentItem.itemName}: Inventory full!");
            

        // Reset player stats
        if (playerController != null && currentItemStats != null)
        {
            Loot.EquipmentType lootType = ConvertToLootType(currentItem.itemType);
            playerController.UnequipItemStats(lootType);
        }

        Debug.Log($"[EquipSlot] Unequipped {currentItem.itemName} from {acceptedType} slot.");
        Debug.Log($"Inventory before adding back: {string.Join(", ", inventoryController?.GetInventoryNames() ?? new string[0])}");
        Debug.Log($"Slot before clearing: {currentItem?.itemName ?? "empty"}");


        currentItem = null;
        currentItemStats = null;
        UpdateIcon();

         Debug.Log($"Slot after clearing: {currentItem}");
         Debug.Log("Inventory contains: " + string.Join(", ", inventoryController.inventoryItems.Select(i => i.itemName)));
 
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[EquipSlot] Slot clicked by player.");
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

    // Optional: call this after scene load or inventory rebuild
    public void BlockClickTemporarily()
    {
        allowClick = false;
        StartCoroutine(EnableClickNextFrame());
    }

    private System.Collections.IEnumerator EnableClickNextFrame()
    {
        yield return null; // wait 1 frame
        allowClick = true;
        Debug.Log("[EquipSlot] Clicks re-enabled.");
    }



}
