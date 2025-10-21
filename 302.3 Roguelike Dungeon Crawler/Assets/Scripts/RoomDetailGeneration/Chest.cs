using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer))]
public class Chest : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite closedSprite;
    public Sprite openSprite;

    [Header("Interaction")]
    public KeyCode openKey = KeyCode.E;
    public string playerTag = "Player";
    [Header("Loot Event")]
    public GameObject droppedItemPrefab;
    public List<Loot> lootList = new List<Loot>();
    private SpriteRenderer sr;
    private bool playerInRange = false;
    private bool isOpen = false;

    Loot GetDroppedItem()
    {
        int randomNumber = Random.Range(1, 101); // 1 - 100
        List<Loot> possibleItems = new List<Loot>();
        foreach (Loot item in lootList)
        {
            if (randomNumber <= item.dropChance)
            {
                possibleItems.Add(item);
            }
        }
        if (possibleItems.Count > 0)
        {
            Loot droppedItem = possibleItems[Random.Range(0, possibleItems.Count)];
            return droppedItem;
        }
        Debug.Log("No item dropped from chest.");
        return null;
    }

    public void InstantiateLoot(Vector3 spawnPosition)
    {
        Loot droppedItem = GetDroppedItem();
        if (droppedItem != null)
        {
            GameObject lootGameObject = Instantiate(droppedItemPrefab, spawnPosition, Quaternion.identity);

            var sr = lootGameObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = droppedItem.lootSprite;

            // generate equipment stats if this loot is equipment
            EquipmentStats stats = null;
            if (droppedItem.category == Loot.LootCategory.Equipment)
            {
                stats = new EquipmentStats();
                stats.equipmentType = droppedItem.equipmentType;
                if (droppedItem.equipmentType == Loot.EquipmentType.Sword)
                {
                    stats.attackPower = Random.Range(droppedItem.minAttack, droppedItem.maxAttack + 1);
                    stats.attackSpeed = Random.Range(droppedItem.minSpeed, droppedItem.maxSpeed);
                    stats.defense = 0;
                }
                else // Armour
                {
                    stats.defense = Random.Range(droppedItem.minDefense, droppedItem.maxDefense + 1);
                }
            }

            // ensure LootPickup component exists and initialize it
            var pickup = lootGameObject.GetComponent<LootPickup>();
            if (pickup == null) pickup = lootGameObject.AddComponent<LootPickup>();
            pickup.Init(droppedItem, stats);

            float dropForce = 100f;
            Vector2 dropDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));

            var rb = lootGameObject.GetComponent<Rigidbody2D>();
            if (rb == null) rb = lootGameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.AddForce(dropDirection.normalized * dropForce, ForceMode2D.Impulse);

            // stop motion after 1 second so the item doesn't keep sliding
            StartCoroutine(StopLootMotion(rb, 1f));

            // ensure collider exists and is trigger for pickup
            var col = lootGameObject.GetComponent<Collider2D>();
            if (col == null)
            {
                var c = lootGameObject.AddComponent<CircleCollider2D>();
                c.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }
        }
    }

    private IEnumerator StopLootMotion(Rigidbody2D rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb == null) yield break;
        // use linearVelocity (non-obsolete) to stop motion and sleep the body
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.Sleep();
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (closedSprite != null) sr.sprite = closedSprite;

        // ensure there is at least one non-trigger collider (blocking) and one trigger collider (interaction)
        var cols = GetComponents<Collider2D>();
        bool hasBlocking = false;
        bool hasTrigger = false;
        foreach (var c in cols)
        {
            if (c.isTrigger) hasTrigger = true;
            else hasBlocking = true;
        }

        if (!hasBlocking)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = false;
        }

        if (!hasTrigger)
        {
            var trig = gameObject.AddComponent<CircleCollider2D>();
            trig.isTrigger = true;
            // adjust radius/offset in the inspector if needed
        }
    }

    void Update()
    {
        if (isOpen) return;
        if (playerInRange && Input.GetKeyDown(openKey))
        {
            Open();
        }
    }

    private void Open()
    {
        isOpen = true;
        if (openSprite != null) sr.sprite = openSprite;

        // disable only trigger colliders (interaction) so the chest remains solid
        var colliders = GetComponents<Collider2D>();
        foreach (var c in colliders)
        {
            if (c.isTrigger)
                c.enabled = false;
        }

        InstantiateLoot(transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) playerInRange = false;
    }
}
