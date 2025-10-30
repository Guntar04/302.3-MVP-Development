using UnityEngine;
using UnityEngine.UI;

public class EquipSlot : MonoBehaviour
{
    [Header("Slot Info")]
    public ItemType acceptedType;               // e.g. Helmet, Chestplate, Shield
    public Image itemIcon;                      // icon for the equipped item
    public PlayerStats playerStats;             // reference to PlayerStats
    public InventoryManager inventoryManager;   // reference to return items to inventory
    public PlayerHUD playerHUD;                 // reference to update shield/health bars

    private ItemData currentItem;               // currently equipped item

    private void Awake()
    {
        ClearSlot(); // start empty
    }

    /// <summary>
    /// Try to equip a new item
    /// </summary>
    public bool AcceptItem(ItemData newItem)
    {
        if (newItem == null) return false;

        if (newItem.itemType != acceptedType)
        {
            Debug.Log($"{newItem.itemName} cannot be equipped in {acceptedType} slot!");
            return false; // wrong type
        }

        // Unequip current item first
        if (currentItem != null)
            Unequip();

        // Equip the new item
        currentItem = newItem;
        UpdateIcon();

        // Update player stats
        if (playerStats != null)
            playerStats.EquipItem(newItem);

        // Update HUD if needed
        if (playerHUD != null)
            playerHUD.UpdateHUD();

        Debug.Log($"Equipped {newItem.itemName} in {acceptedType} slot!");
        return true;
    }

    /// <summary>
    /// Unequip current item and return to inventory
    /// </summary>
    public void Unequip()
{
    if (currentItem == null) return;

    // Return to inventory
    if (inventoryManager != null)
        inventoryManager.AddItem(currentItem);

    // Update player stats
    if (playerStats != null)
    {
        playerStats.UnequipItem(currentItem); // important for shield/health recalculation
        // playerStats.NotifyStatsChanged(); // not needed if UnequipItem already calls it
    }

    ClearSlot();
}


    /// <summary>
    /// Clear slot visually and logically
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
    }

    /// <summary>
    /// Get the currently equipped item
    /// </summary>
    public ItemData GetEquippedItem() => currentItem;

    /// <summary>
    /// Update icon based on current item
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

    /// <summary>
    /// Call this when the slot is clicked
    /// </summary>
    public void OnClickSlot()
    {
        if (currentItem != null)
        {
            Unequip();
        }
    }
}
