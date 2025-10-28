using UnityEngine;
using UnityEngine.UI;

public class EquipSlot : MonoBehaviour
{
    [Header("Slot Info")]
    public ItemType acceptedType;          // e.g. Helmet, Armor, Weapon
    public Image itemIcon;                 // icon for the equipped item
    public PlayerStats playerStats;        // reference to PlayerStats
    public InventoryManager inventoryManager; // reference for returning items to inventory

    private ItemData currentItem;          // currently equipped item

    private void Awake()
    {
        ClearSlot(); // make sure the slot looks empty at start
    }

    /// <summary>
    /// Try to equip an item. Returns true if successful.
    /// </summary>
    public bool AcceptItem(ItemData newItem)
    {
        if (newItem == null)
            return false;

        if (newItem.itemType != acceptedType)
        {
            Debug.Log($"{newItem.itemName} cannot be equipped in {acceptedType} slot!");
            return false; // wrong type, reject
        }

        // If slot already has an item, send it back to inventory
        if (currentItem != null && inventoryManager != null)
        {
            inventoryManager.AddItem(currentItem);
        }

        // Equip the new item
        currentItem = newItem;
        UpdateIcon();

        // Update player stats and HUD
        if (playerStats != null)
            playerStats.RecalculateStats();


        Debug.Log($"Equipped {newItem.itemName} in {acceptedType} slot!");
        return true;
    }

    /// <summary>
    /// Unequip current item and return to inventory
    /// </summary>
    public void Unequip()
    {
        if (currentItem != null && inventoryManager != null)
        {
            inventoryManager.AddItem(currentItem);
        }

        ClearSlot();

        // Update player stats and HUD
        if (playerStats != null)
            playerStats.RecalculateStats();

    }

    /// <summary>
    /// Clears the slot visually and logically
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false; // hide icon when empty
        }
    }

    /// <summary>
    /// Get the currently equipped item
    /// </summary>
    public ItemData GetEquippedItem() => currentItem;

    /// <summary>
    /// Updates the visual icon based on currentItem
    /// </summary>
    private void UpdateIcon()
    {
        if (itemIcon == null) return;

        if (currentItem != null)
        {
            itemIcon.sprite = currentItem.icon;
            itemIcon.enabled = true;
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
    }
}
