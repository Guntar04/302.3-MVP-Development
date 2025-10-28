using UnityEngine;
using UnityEngine.UI;

public class EquipSlot : MonoBehaviour
{
    [Header("Slot Info")]
    public ItemType acceptedType;          // e.g. Helmet, Armor, Weapon
    public Image itemIcon;                 // the icon that shows the equipped item
    public InventoryManager inventoryManager; // reference for unequipping items

    private ItemData currentItem;          // the item currently equipped

    private void Awake()
    {
        // Make sure slot looks empty at start
        ClearSlot();
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
