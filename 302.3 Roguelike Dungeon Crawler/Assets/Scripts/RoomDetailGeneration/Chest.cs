using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // added

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
    public GameObject droppedItemPrefab; // Loot prefab
    public GameObject healthPotPrefab;  // HealthPot prefab
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
        // Drop loot prefab
        Loot droppedItem = GetDroppedItem();
        if (droppedItem != null && droppedItemPrefab != null)
        {
            GameObject lootGameObject = Instantiate(droppedItemPrefab, spawnPosition, Quaternion.identity);
            lootGameObject.name = "Loot_" + droppedItem.lootName;

            // ensure there's a visible sprite
            var sr = lootGameObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = droppedItem.lootSprite;

            // ---------- physics collider setup ----------
            // Ensure we have a non-trigger collider for physics collisions (bounces)
            Collider2D mainCol = lootGameObject.GetComponent<Collider2D>();
            if (mainCol == null)
            {
                mainCol = lootGameObject.AddComponent<CircleCollider2D>();
            }
            mainCol.isTrigger = false; // physics collider

            // Also add a separate trigger collider (same object) for pickup detection
            CircleCollider2D pickupCol = lootGameObject.AddComponent<CircleCollider2D>();
            pickupCol.isTrigger = true;
            pickupCol.radius = Mathf.Max(0.2f, (mainCol as CircleCollider2D)?.radius ?? 0.5f) * 0.8f;

            // Give the collider a bouncy physics material so it reflects off walls instead of leaving bounds
            PhysicsMaterial2D physMat = new PhysicsMaterial2D();
            physMat.bounciness = 0.6f;
            physMat.friction = 0.4f;
            if (mainCol is CircleCollider2D cc) cc.sharedMaterial = physMat;
            else mainCol.sharedMaterial = physMat;

            // ---------- Rigidbody setup ----------
            var rb = lootGameObject.GetComponent<Rigidbody2D>();
            if (rb == null) rb = lootGameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.mass = 1f;
            rb.linearDamping = 1f; // a bit of damping so item doesn't slide forever
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // ---------- initialize LootPickup component ----------
            var pickup = lootGameObject.GetComponent<LootPickup>();
            if (pickup == null) pickup = lootGameObject.AddComponent<LootPickup>();
            pickup.Init(droppedItem, null); // if you want equipment stats, set them here

            // ---------- smaller, controlled drop force ----------
            // reduced spread so loot stays near chest
            float dropForceMin = 2f;
            float dropForceMax = 6f;
            Vector2 dropDirection = new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(0.25f, 0.7f));
            dropDirection.Normalize();
            float dropForce = Random.Range(dropForceMin, dropForceMax);
            rb.AddForce(dropDirection * dropForce, ForceMode2D.Impulse);

            // ensure loot does NOT collide with the player's colliders (player can walk over)
            var player = GameObject.FindWithTag(playerTag);
            if (player != null)
            {
                var playerCols = player.GetComponentsInChildren<Collider2D>();
                foreach (var pcol in playerCols)
                {
                    // ignore collisions between the player's colliders and the physics (non-trigger) collider only
                    if (pcol != null) Physics2D.IgnoreCollision(mainCol, pcol, true);
                    // important: DO NOT ignore the pickup trigger (pickupCol) so OnTriggerEnter still fires
                }
            }

            // Stop motion after a short time so the item doesn't keep sliding
            StartCoroutine(StopLootMotion(rb, 1.0f));

            // ---------- ensure object is cleaned up on scene change ----------
            // attach the standalone LootCleanup component so it will be destroyed when scene changes
            if (lootGameObject.GetComponent<LootCleanup>() == null)
                lootGameObject.AddComponent<LootCleanup>();
        }

        // Drop health potion prefab (unchanged, but keep local small offset)
        if (healthPotPrefab != null)
        {
            Vector3 healthPotPosition = spawnPosition + new Vector3(0.15f, 0.15f, 0); // smaller offset
            var hp = Instantiate(healthPotPrefab, healthPotPosition, Quaternion.identity);
            if (hp.GetComponent<LootCleanup>() == null) hp.AddComponent<LootCleanup>();
        }
    }

    private IEnumerator StopLootMotion(Rigidbody2D rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb == null) yield break;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.Sleep();
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (closedSprite != null) sr.sprite = closedSprite;

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

        var colliders = GetComponents<Collider2D>();
        foreach (var c in colliders)
        {
            // disable both trigger and non-trigger colliders so player can walk over the chest
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

    // Helper component to destroy loot when a new scene loads (prevents carry-over)
    private class LootCleanup : MonoBehaviour
    {
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Destroy(gameObject);
        }
    }
}
