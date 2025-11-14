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

        var sr = GetComponent<SpriteRenderer>();
    if (sr != null)
    {
        // Add this loot sprite to GameData
        if (!GameData.CollectedLoot.Contains(sr.sprite)) // optional: prevent duplicates
            GameData.CollectedLoot.Add(sr.sprite);
    }

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

    // --- 1. Generate EquipmentStats from Loot ---
    EquipmentStats statsForItem = new EquipmentStats
    {
        attackPower = lootData.minAttack + Random.Range(0, lootData.maxAttack - lootData.minAttack + 1),
        defense = lootData.minDefense + Random.Range(0, lootData.maxDefense - lootData.minDefense + 1),
        attackSpeed = lootData.minAttackSpeed + Random.Range(0f, lootData.maxAttackSpeed - lootData.minAttackSpeed + 1)
    };

    // --- 2. Convert Loot → ItemData ---
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

    // Assign the generated stats to the item
    newItem.equipmentStats = statsForItem;

    // --- 3. Add to inventory ---
InventoryUIController inventory = FindFirstObjectByType<InventoryUIController>();
    if (inventory != null)
    {
        inventory.AddItem(newItem);
    }

    // --- 4. Auto-equip if possible ---
    PlayerController pc = playerObj.GetComponent<PlayerController>();
    if (pc != null)
    {
        EquipSlot[] slots = playerObj.GetComponentsInChildren<EquipSlot>();
        foreach (var slot in slots)
        {
            Loot.EquipmentType lootType = lootData.equipmentType;

            if ((slot.acceptedType == ItemType.Weapon && lootType == Loot.EquipmentType.Sword) ||
    (slot.acceptedType == ItemType.Chestplate && lootType == Loot.EquipmentType.Chestplate) ||
    (slot.acceptedType == ItemType.Helmet && lootType == Loot.EquipmentType.Helmet) ||
    (slot.acceptedType == ItemType.Pants && lootType == Loot.EquipmentType.Pants) ||
    (slot.acceptedType == ItemType.Boots && lootType == Loot.EquipmentType.Boots) ||
    (slot.acceptedType == ItemType.Shield && lootType == Loot.EquipmentType.Shield))

            {
                if (slot.AcceptItem(newItem, statsForItem, lootType))
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

    // --- 5. Invoke pickup event and destroy loot object ---
    OnPickedUp?.Invoke();
    Destroy(gameObject);
    Debug.Log($"Picked up {lootData.lootName}");
}


}

