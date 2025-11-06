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
    if (lootData == null)
    {
        Debug.LogWarning("LootPickup: LootData missing!");
        return;
    }

    // --- 1. Convert Loot → ItemData ---
    ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
    newItem.itemName = lootData.lootName;
    newItem.icon = lootData.lootSprite;

    // Map LootCategory / EquipmentType → ItemType
    if (lootData.category == Loot.LootCategory.Equipment)
    {
        newItem.itemType = lootData.equipmentType == Loot.EquipmentType.Sword ? ItemType.Weapon : ItemType.Chestplate;
    }
    else
    {
        newItem.itemType = ItemType.Consumable;
    }

    // --- 2. Generate EquipmentStats from Loot ---
    EquipmentStats generatedStats = new EquipmentStats
    {
        attackPower = Random.Range(lootData.minAttack, lootData.maxAttack + 1),
        attackSpeed = Random.Range(lootData.minSpeed, lootData.maxSpeed),
        defense = Random.Range(lootData.minDefense, lootData.maxDefense + 1)
    };

    newItem.equipmentStats = generatedStats; // assign stats to the item

    // --- 3. Add to inventory ---
    InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
    if (inventory != null)
    {
        inventory.AddItem(newItem);
    }

    // --- 4. Auto-equip if possible ---
    PlayerController pc = playerObj.GetComponent<PlayerController>();
    if (pc != null && newItem.equipmentStats != null)
    {
        // Find appropriate slot
        EquipSlot[] slots = playerObj.GetComponentsInChildren<EquipSlot>();
        foreach (var slot in slots)
        {
            if ((slot.acceptedType == ItemType.Weapon && lootData.equipmentType == Loot.EquipmentType.Sword) ||
                (slot.acceptedType == ItemType.Chestplate && lootData.equipmentType == Loot.EquipmentType.Armour))
            {
                if (slot.AcceptItem(newItem, newItem.equipmentStats, lootData.equipmentType))
                {
                    Debug.Log($"Auto-equipped {newItem.itemName} in {slot.acceptedType} slot.");
                }
                else
                {
                    Debug.LogWarning($"Failed to auto-equip {newItem.itemName} in {slot.acceptedType} slot.");
                }
                break; // equip only once
            }
        }
    }

    OnPickedUp?.Invoke();
    Destroy(gameObject);
    Debug.Log($"Picked up {lootData.lootName}");
}

}

