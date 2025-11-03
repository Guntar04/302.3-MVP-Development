using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer))]
public class LootPickup : MonoBehaviour
{
    public Loot lootData;
    public EquipmentStats stats;
    public string playerTag = "Player";

    [Header("Pickup")]
    public KeyCode pickupKey = KeyCode.E;
    public UnityEvent OnPickedUp;
    public UnityEvent OnPlayerEnterRange; // optional: hook UI show
    public UnityEvent OnPlayerExitRange;  // optional: hook UI hide

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
        if (sr != null && data != null && data.lootSprite != null) sr.sprite = data.lootSprite;

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
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null)
        {
            Debug.Log($"LootPickup: Player picked up {lootData?.lootName} with stats: {stats}");
            var method = pc.GetType().GetMethod("OnPickupLoot");
            if (method != null)
                method.Invoke(pc, new object[] { lootData, stats });
        }

        OnPickedUp?.Invoke();
        Destroy(gameObject);
    }
}
