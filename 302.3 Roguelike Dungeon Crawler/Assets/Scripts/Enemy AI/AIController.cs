using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class AIController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 10; // Maximum health of the enemy
    public int attackPower = 1; // Damage dealt to the player per attack

    [Header("Combat")]
    public float attackRange = 1.5f; // Range at which the enemy can attack
    public float attackCooldown = 1f; // Time between attacks
    public float attackDuration = 0.5f; // Duration of the attack animation

    [Header("Detection & Movement")]
    public float detectionRange = 4f; // Range at which the enemy detects the player
    public float moveSpeed = 2f; // Movement speed of the enemy

    [Header("Stun / Death")]
    public float hitStunDuration = 2f; // Duration of stun when hit
    [SerializeField] private float destroyDelay = 3f; // Delay before destroying the enemy after death

    [Header("Animation")]
    [SerializeField] private Animator animator; // Reference to the Animator component

    [Header("Drops")]
    public GameObject healthPotPrefab; // Prefab for health potion
    [Range(0f, 1f)] public float healthDropChance = 0.333f; // The % chance to drop a health potion
    public GameObject exitKeyPrefab; // Prefab for exit key
    [Range(0f, 1f)] public float exitKeyDropChance = 0.01f; // The % chance to drop an exit key

    [SerializeField] private GameObject hpBarPrefab; // Prefab for the HP bar
    private GameObject hpBarInstance; // Instance of the HP bar
    private RectTransform greenBar; // Reference to the green bar
    private RectTransform redBar; // Reference to the red bar

    private int currentHealth; // Current health of the enemy
    private Transform player; // Reference to the player's transform
    private Rigidbody2D rb; // Reference to the Rigidbody2D component
    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDead = false;
    private float attackTimer = 0f;

    private Vector3 baseScale;
    private static readonly int ParamIsMoving = Animator.StringToHash("isMoving");
    private static readonly int ParamHit = Animator.StringToHash("Hit");
    private static readonly int ParamAttack = Animator.StringToHash("Attack");
    private static readonly int ParamDeath = Animator.StringToHash("Death");

    private bool shouldMoveFlag = false;
    private bool hitTriggered = false;
    private bool guaranteeExitDrop = false;

    [Header("Attack Settings")]
    public LayerMask playerLayerMask = Physics2D.DefaultRaycastLayers;
    // store previous body type while performing an attack (to prevent physics push)
    private RigidbodyType2D savedBodyType = RigidbodyType2D.Dynamic;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;

        // fallback: if no layer mask was assigned, use all layers so tag-checking still works
        if (playerLayerMask == 0)
            playerLayerMask = Physics2D.DefaultRaycastLayers;
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (rb != null) rb.simulated = true;
        if (animator != null) animator.applyRootMotion = false;
        TryAssignPlayer();
    }

    void Update()
    {
        if (isDead) return;

        if (player == null) TryAssignPlayer();

        attackTimer -= Time.deltaTime;

        if (player == null)
        {
            shouldMoveFlag = false;
            UpdateAnimationParameters();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        Collider2D[] hits;

        hits = Physics2D.OverlapCircleAll(transform.position, attackRange, playerLayerMask);

        bool playerInAttackRange = false;
        foreach (var h in hits)
        {
            if (h == null) continue;
            if (h.CompareTag("Player") || (h.attachedRigidbody != null && h.attachedRigidbody.gameObject.CompareTag("Player")))
            {
                playerInAttackRange = true;
                break;
            }
        }

        // move only when not dead/stunned/attacking and player is within detection range but NOT in attack range
        shouldMoveFlag = !isDead && !isStunned && !isAttacking && distance <= detectionRange && !playerInAttackRange;

        // if player is within attackRange and attack timer ready -> start attack
        if (!isStunned && !isAttacking && playerInAttackRange && attackTimer <= 0f)
        {
            StartCoroutine(Attack());
            attackTimer = attackCooldown;
        }

        UpdateAnimationParameters();
        FlipSprite();

        // Update HP bar position
        if (hpBarInstance != null)
        {
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 1, 0));
            hpBarInstance.GetComponent<RectTransform>().position = screenPosition;
        }
    }

    private void TryAssignPlayer()
    {
        var dd = UnityEngine.Object.FindAnyObjectByType<DungeonData>();
        if (dd != null && dd.PlayerReference != null)
        {
            player = dd.PlayerReference.transform;
            return;
        }
        var pgo = GameObject.FindWithTag("Player");
        if (pgo != null) player = pgo.transform;
    }

    private void UpdateAnimationParameters()
    {
        if (animator == null) return;

        animator.SetBool(ParamIsMoving, shouldMoveFlag);

        if (isDead)
        {
            animator.SetBool(ParamIsMoving, false);
            animator.SetTrigger(ParamDeath);
        }
    }

    private IEnumerator Attack()
    {
        if (isDead || isStunned) yield break;

        isAttacking = true;
        shouldMoveFlag = false;
        UpdateAnimationParameters();

        if (rb != null)
        {
            savedBodyType = rb.bodyType;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        if (animator != null) animator.SetTrigger(ParamAttack);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, playerLayerMask);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.CompareTag("Player") || (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject.CompareTag("Player")))
            {
                var pc = hit.GetComponent<PlayerController>() ?? hit.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    var method = pc.GetType().GetMethod("ApplyHit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (method != null)
                    {
                        try { method.Invoke(pc, new object[] { attackPower, 0.05f }); }
                        catch { }
                    }
                    else
                    {
                        var field = pc.GetType().GetField("health", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (field != null && field.FieldType == typeof(int))
                        {
                            int prev = (int)field.GetValue(pc);
                            field.SetValue(pc, prev - attackPower);
                        }
                    }
                }
                break;
            }
        }

        float t = 0f;
        while (t < attackDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = savedBodyType;
        }

        isAttacking = false;
        hitTriggered = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth > 0)
        {
            // Show the HP bar if it hasn't been instantiated yet
            if (hpBarInstance == null)
            {
                if (hpBarPrefab == null)
                {
                    Debug.LogError("HP Bar Prefab is not assigned!");
                    return;
                }

                var canvas = GameObject.Find("Canvas");
                if (canvas == null)
                {
                    Debug.LogError("Canvas not found in the scene!");
                    return;
                }

                // Instantiate the HP bar as a child of the Canvas
                hpBarInstance = Instantiate(hpBarPrefab, canvas.transform);
                greenBar = hpBarInstance.transform.Find("GreenBar")?.GetComponent<RectTransform>();
                redBar = hpBarInstance.transform.Find("RedBar")?.GetComponent<RectTransform>();

                if (greenBar == null || redBar == null)
                {
                    Debug.LogError("GreenBar or RedBar is missing in the HP Bar prefab!");
                    return;
                }

                // Set the position of the HP bar relative to the enemy
                Vector2 screenPosition = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 1, 0));
                hpBarInstance.GetComponent<RectTransform>().position = screenPosition;
            }

            // Update the HP bar
            UpdateHPBar();

            // Trigger hit animation
            if (!hitTriggered)
            {
                hitTriggered = true;
                if (animator != null) animator.SetTrigger(ParamHit);
            }

            if (!isStunned) StartCoroutine(HitStunCoroutine(hitStunDuration));
        }
        else
        {
            Die();
        }
    }

    private void UpdateHPBar()
    {
        if (hpBarPrefab == null)
        {
            Debug.LogError("HP Bar Prefab is not assigned!");
            return;
        }
        if (greenBar == null || redBar == null)
        {
            Debug.LogError("GreenBar or RedBar is missing in the HP Bar prefab!");
            return;
        }

        float healthPercentage = (float)currentHealth / maxHealth;

        // Update the green bar (remaining HP)
        greenBar.GetComponent<Image>().fillAmount = healthPercentage;

        // Update the red bar (missing HP)
        redBar.GetComponent<Image>().fillAmount = 1 - healthPercentage;
    }

    private IEnumerator HitStunCoroutine(float duration)
    {
        // start stun: stop movement and clear moving param so Run/Idle transitions update immediately
        isStunned = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool(ParamIsMoving, false);

        yield return new WaitForSeconds(duration);

        // end stun: allow future hit triggers
        isStunned = false;
        hitTriggered = false;

        // refresh player reference and recompute movement intent immediately so animator can switch to Run without delay
        TryAssignPlayer();
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            shouldMoveFlag = !isDead && !isStunned && !isAttacking && distance <= detectionRange && distance > attackRange;
        }
        else
        {
            shouldMoveFlag = false;
        }

        // update animator right away
        UpdateAnimationParameters();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        StopAllCoroutines();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;

        if (animator != null)
        {
            animator.SetBool(ParamIsMoving, false);
            animator.SetTrigger(ParamDeath);
        }

        // Destroy the HP bar
        if (hpBarInstance != null)
        {
            Destroy(hpBarInstance);
        }

        StartCoroutine(WaitForDeathAndDestroy(destroyDelay));
    }

    private IEnumerator WaitForDeathAndDestroy(float fallback)
    {
        if (animator == null)
        {
            // If no animator, spawn drop(s) immediately then destroy after fallback
            if (healthPotPrefab != null && Random.value <= healthDropChance)
                Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

            // EXIT KEY: only spawn if not already spawned this level
            if (exitKeyPrefab != null && (guaranteeExitDrop || Random.value <= exitKeyDropChance))
            {
                var dd0 = UnityEngine.Object.FindAnyObjectByType<DungeonData>();
                if (dd0 == null || dd0.ExitKeySpawned == false)
                {
                    Instantiate(exitKeyPrefab, transform.position, Quaternion.identity);
                    if (dd0 != null) dd0.ExitKeySpawned = true;
                }
            }

            yield return new WaitForSeconds(fallback);
            Destroy(gameObject);
            yield break;
        }

        int layer = 0;
        int deathHash = Animator.StringToHash("Death");
        float elapsed = 0f;
        const float pollInterval = 0.02f;

        // wait until animator enters the Death state (with a short fallback)
        while (elapsed < fallback)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.shortNameHash == deathHash) break;
            elapsed += pollInterval;
            yield return new WaitForSeconds(pollInterval);
        }

        // if we never entered the death state, fallback-destroy now (spawn drop immediately)
        var current = animator.GetCurrentAnimatorStateInfo(layer);
        if (current.shortNameHash != deathHash)
        {
            if (healthPotPrefab != null && Random.value <= healthDropChance)
                Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

            // exit key fallback: respect per-level flag and guarantee flag
            if (exitKeyPrefab != null && (guaranteeExitDrop || Random.value <= exitKeyDropChance))
            {
                var dd1 = UnityEngine.Object.FindAnyObjectByType<DungeonData>();
                if (dd1 == null || dd1.ExitKeySpawned == false)
                {
                    Instantiate(exitKeyPrefab, transform.position, Quaternion.identity);
                    if (dd1 != null) dd1.ExitKeySpawned = true;
                }
            }

            Destroy(gameObject);
            yield break;
        }

        // wait until the Death state's playback has reached its end (normalizedTime >= 1)
        while (true)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.shortNameHash == deathHash && info.normalizedTime >= 1f)
            {
                // spawn health pot (chance)
                if (healthPotPrefab != null && Random.value <= healthDropChance)
                    Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

                // spawn exit key only if not already spawned this level and either guaranteed or passed chance
                if (exitKeyPrefab != null && (guaranteeExitDrop || Random.value <= exitKeyDropChance))
                {
                    var dd2 = UnityEngine.Object.FindAnyObjectByType<DungeonData>();
                    if (dd2 == null || dd2.ExitKeySpawned == false)
                    {
                        Instantiate(exitKeyPrefab, transform.position, Quaternion.identity);
                        if (dd2 != null) dd2.ExitKeySpawned = true;
                    }
                }

                // prevent Animator from snapping back to Idle by disabling it before destruction
                animator.enabled = false;
                Destroy(gameObject);
                yield break;
            }

            // safety timeout: if somehow it never finishes, break after fallback
            elapsed += pollInterval;
            if (elapsed >= fallback)
            {
                if (healthPotPrefab != null && Random.value <= healthDropChance)
                    Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

                if (exitKeyPrefab != null && (guaranteeExitDrop || Random.value <= exitKeyDropChance))
                {
                    var dd3 = UnityEngine.Object.FindAnyObjectByType<DungeonData>();
                    if (dd3 == null || dd3.ExitKeySpawned == false)
                    {
                        Instantiate(exitKeyPrefab, transform.position, Quaternion.identity);
                        if (dd3 != null) dd3.ExitKeySpawned = true;
                    }
                }

                Destroy(gameObject);
                yield break;
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private void FlipSprite()
    {
        if (player == null) return;
        transform.localScale = player.position.x < transform.position.x
            ? new Vector3(-baseScale.x, baseScale.y, baseScale.z)
            : baseScale;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (isDead || isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // apply movement when intent says to move
        if (shouldMoveFlag)
        {
            Vector2 direction = ((Vector2)player.position - rb.position).normalized;

            if (rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.linearVelocity = direction * moveSpeed;
            }
            else
            {
                Vector2 newPos = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
                rb.MovePosition(newPos);
            }
        }
        else
        {
            // stop cleanly
            rb.linearVelocity = Vector2.zero;
            if (rb.bodyType != RigidbodyType2D.Dynamic)
                rb.MovePosition(rb.position);
        }
    }

    // draw attack range in editor to help tuning
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}