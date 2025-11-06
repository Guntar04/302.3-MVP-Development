using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer))]
public class LootPickup : MonoBehaviour
{
    public Loot lootData;                  // The loot prefab data
    public EquipmentStats stats;           // Generated stats for this loot
    public string playerTag = "Player";

    [Header("Pickup")]
    public KeyCode pickupKey = KeyCode.E;
    public UnityEvent OnPickedUp;
    public UnityEvent OnPlayerEnterRange; // optional: show UI
    public UnityEvent OnPlayerExitRange;  // optional: hide UI

    private Collider2D col;
    private GameObject playerInRangeObj;
    private bool isPlayerInRange;

    void Reset()
    {
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Init(Loot data, EquipmentStats generatedStats)
    {
        lootData = data;
        stats = generatedStats;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && data != null && data.lootSprite != null)
            sr.sprite = data.lootSprite;

        gameObject.name = data != null ? $"Loot_{data.lootName}" : "Loot";
    }

    private void Update()
    {
        if (!isPlayerInRange || playerInRangeObj == null) return;

        if (Input.GetKeyDown(pickupKey))
        {
            TryPickup(playerInRangeObj);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRangeObj = other.gameObject;
        isPlayerInRange = true;
        OnPlayerEnterRange?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (other.gameObject == playerInRangeObj)
        {
            playerInRangeObj = null;
            isPlayerInRange = false;
            OnPlayerExitRange?.Invoke();
        }
    }

    private void TryPickup(GameObject playerObj)
    {
        // --- 1. Add to InventoryManager ---
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory != null && lootData != null)
        {
            // Convert Loot → ItemData for inventory
            ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
            newItem.itemName = lootData.lootName;
            newItem.icon = lootData.lootSprite;

            // Assign type based on loot category & equipment type
            if (lootData.category == Loot.LootCategory.Equipment)
            {
                switch (lootData.equipmentType)
                {
                    case Loot.EquipmentType.Sword: newItem.itemType = ItemType.Weapon; break;
                    case Loot.EquipmentType.Armour: newItem.itemType = ItemType.Chestplate; break;
                    default: newItem.itemType = ItemType.Misc; break;
                }
            }
            else
            {
                newItem.itemType = ItemType.Consumable;
            }

            // Add stats from EquipmentStats if available
            if (stats != null)
            {
                newItem.healthBonus = 0;
                newItem.shieldBonus = stats.defense; // or assign appropriately
            }

            inventory.AddItem(newItem);
            Debug.Log($"Picked up {lootData.lootName} and added to inventory!");
        }
        else
        {
            Debug.LogWarning("No InventoryManager found or LootData missing!");
        }

        // --- 2. Keep exhausting loot logic ---
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null && lootData != null)
        {
            var method = pc.GetType().GetMethod("OnPickupLoot");
            if (method != null)
                method.Invoke(pc, new object[] { lootData, stats });
        }

        OnPickedUp?.Invoke();
        Destroy(gameObject);
    }
}
