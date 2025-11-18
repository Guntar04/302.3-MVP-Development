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

    [Header("HP Bar")]
    public GameObject hpBarPrefab;
    public Transform hpAnchor;           // optional: assign a child transform that follows the sprite/animation
    public Vector3 hpOffset = new Vector3(0f, 1f, 0f); // fallback offset if no anchor

    [Header("Enemy Info")]
public string enemyName = "Skullman";
public Sprite enemySprite; // assign in the inspector

  public static int enemiesKilled = 0;

    private GameObject hpBarInstance;
    private RectTransform greenBar;
    private RectTransform redBar;

    // cached canvas for UI positioning
    private Canvas cachedCanvas;
    private RectTransform cachedCanvasRect;
    private Camera cachedCanvasCamera;

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
    // new field to remember if this death must guarantee the exit key
    private bool guaranteeExitDrop = false;

    [Header("Debug")]
    public bool debugDraw = false;
    public float debugRayDistance = 0.6f;

    [Header("Attack Settings")]
    public LayerMask playerLayerMask = Physics2D.DefaultRaycastLayers;
    // store previous body type while performing an attack (to prevent physics push)
    private RigidbodyType2D savedBodyType = RigidbodyType2D.Dynamic;
    
    [Header("Range Offsets")]
    // Offsets allow the attack/detection circles to be shifted relative to the
    // enemy root position (useful when sprite visuals are offset from physics root)
    public Vector2 attackRangeOffset = Vector2.zero;
    public Vector2 detectionRangeOffset = Vector2.zero;

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
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;

        // cache canvas and camera once
        cachedCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (cachedCanvas != null)
        {
            cachedCanvasRect = cachedCanvas.GetComponent<RectTransform>();
            cachedCanvasCamera = (cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cachedCanvas.worldCamera ?? Camera.main;
        }

        // If no explicit hpAnchor assigned, try to find a child named "HPAnchor" (create in prefab under the animated graphics)
        if (hpAnchor == null)
        {
            var found = transform.Find("HPAnchor");
            if (found != null) hpAnchor = found;
            else
            {
                // try common child name "Graphics" then its child "HPAnchor"
                var g = transform.Find("Graphics");
                if (g != null)
                {
                    var h = g.Find("HPAnchor");
                    if (h != null) hpAnchor = h;
                }
            }
        }

        currentHealth = maxHealth;
        if (rb != null) rb.simulated = true;
        if (animator != null) animator.applyRootMotion = false;
        TryAssignPlayer();
    }

    void Update()
    {
        // Create HP bar lazily if needed
        if (currentHealth < maxHealth) CreateHpBarIfNeeded();

        // Update fills and position every frame so it follows animation (alive or dying)
        UpdateHPBar();
        UpdateHPBarPosition();

        if (isDead) return;

        if (player == null) TryAssignPlayer();

        attackTimer -= Time.deltaTime;

        if (player == null)
        {
            shouldMoveFlag = false;
            UpdateAnimationParameters();
            return;
        }

    // measure distance from the detection center (allows offsetting the detection
    // circle if visuals/anchor are not at the physics root)
    Vector2 detectionCenter = (Vector2)transform.position + detectionRangeOffset;
    float distance = Vector2.Distance(detectionCenter, player.position);

        Collider2D[] hits;

    // check attack hits around the attack center (supports an offset)
    Vector2 attackCenter = (Vector2)transform.position + attackRangeOffset;
    hits = Physics2D.OverlapCircleAll(attackCenter, attackRange, playerLayerMask);

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

    // use the same attack center offset when performing attack checks
    Vector2 attackCenterLocal = (Vector2)transform.position + attackRangeOffset;
    Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenterLocal, attackRange, playerLayerMask);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.CompareTag("Player"))
            {
                var playerController = hit.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.TakeDamage(attackPower, this); // Directly call TakeDamage
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
            CreateHpBarIfNeeded();

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

    // Ensure HP bar exists (create and cache references)
    private void CreateHpBarIfNeeded()
    {
        if (hpBarInstance != null || hpBarPrefab == null || cachedCanvas == null) return;

        hpBarInstance = Instantiate(hpBarPrefab, cachedCanvas.transform);
        // preserve prefab layout/scaling
        hpBarInstance.transform.SetParent(cachedCanvas.transform, false);

        greenBar = hpBarInstance.transform.Find("GreenBar")?.GetComponent<RectTransform>();
        redBar = hpBarInstance.transform.Find("RedBar")?.GetComponent<RectTransform>();
    }

    private void UpdateHPBar()
    {
        if (hpBarInstance == null || greenBar == null || redBar == null) return;

        float healthPercentage = Mathf.Clamp01((float)currentHealth / (float)maxHealth);
        var greenImg = greenBar.GetComponent<Image>();
        var redImg = redBar.GetComponent<Image>();
        if (greenImg != null) greenImg.fillAmount = healthPercentage;
        if (redImg != null) redImg.fillAmount = 1f - healthPercentage;
    }

    private void UpdateHPBarPosition()
    {
        if (hpBarInstance == null || cachedCanvasRect == null) return;

        // Use hpAnchor if provided (should be a child that moves with animation), otherwise fallback to transform.position + offset
        Vector3 worldPos;
        if (hpAnchor != null) worldPos = hpAnchor.position;
        else worldPos = transform.position + hpOffset;

        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(cachedCanvasRect, screenPoint, cachedCanvasCamera, out localPoint);

        var rt = hpBarInstance.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = localPoint;
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

         if (LevelManager.Instance != null)
    {
        LevelManager.Instance.RegisterEnemyKill();
    }

        StopAllCoroutines();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;

        // keep HP bar visible and anchored to corpse until object is destroyed
        // Do NOT destroy hpBarInstance here.

        if (animator != null)
        {
            animator.SetBool(ParamIsMoving, false);
            animator.SetTrigger(ParamDeath);
        }

        StartCoroutine(WaitForDeathAndDestroy(destroyDelay));
    }

    private IEnumerator WaitForDeathAndDestroy(float fallback)
    {
        // helper to spawn exit key safely (sets DungeonData flag)
        System.Action SpawnExitKeySafe = () =>
        {
            if (exitKeyPrefab == null)
            {
                Debug.LogWarning("AIController: exitKeyPrefab is null, cannot spawn exit key.");
                return;
            }

            var dd = FindFirstObjectByType<DungeonData>();
            if (dd != null && dd.ExitKeySpawned)
            {
                Debug.Log("AIController: ExitKeySpawned already true, skipping spawn.");
                return;
            }

            Instantiate(exitKeyPrefab, transform.position, Quaternion.identity);
            if (dd != null) dd.ExitKeySpawned = true;
            Debug.Log("AIController: Exit key spawned by " + name);
        };

        if (animator == null)
        {
            // immediate spawn path
            if (healthPotPrefab != null && Random.value <= healthDropChance)
                Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

            if (guaranteeExitDrop || Random.value <= exitKeyDropChance)
                SpawnExitKeySafe();

            yield return new WaitForSeconds(fallback);
            Destroy(gameObject);
            yield break;
        }

        int layer = 0;
        int deathHash = Animator.StringToHash("Death");
        float elapsed = 0f;
        const float pollInterval = 0.02f;

        // wait until death state, with fallback
        while (elapsed < fallback)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.shortNameHash == deathHash) break;
            elapsed += pollInterval;
            yield return new WaitForSeconds(pollInterval);
        }

        // if no death animation, spawn immediately using same logic
        var current = animator.GetCurrentAnimatorStateInfo(layer);
        if (current.shortNameHash != deathHash)
        {
            if (healthPotPrefab != null && Random.value <= healthDropChance)
                Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

            if (guaranteeExitDrop || Random.value <= exitKeyDropChance)
                SpawnExitKeySafe();

            Destroy(gameObject);
            yield break;
        }

        // wait until death animation finishes
        while (true)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.shortNameHash == deathHash && info.normalizedTime >= 1f)
            {
                if (healthPotPrefab != null && Random.value <= healthDropChance)
                    Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

                if (guaranteeExitDrop || Random.value <= exitKeyDropChance)
                    SpawnExitKeySafe();

                animator.enabled = false;
                Destroy(gameObject);
                yield break;
            }

            elapsed += pollInterval;
            if (elapsed >= fallback)
            {
                if (healthPotPrefab != null && Random.value <= healthDropChance)
                    Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

                if (guaranteeExitDrop || Random.value <= exitKeyDropChance)
                    SpawnExitKeySafe();

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

            // Simple local avoidance: if a direct raycast to the player hits an obstacle
            // (not the player), try to move around it by using a perpendicular offset.
            // This helps avoid getting stuck on single colliders or tile edges.
            RaycastHit2D hit = Physics2D.Raycast(rb.position, direction, debugRayDistance);
            if (debugDraw)
            {
                Debug.DrawRay(rb.position, direction * debugRayDistance, hit.collider == null ? Color.green : Color.red, 0.1f);
            }
            if (hit.collider != null && !hit.collider.CompareTag("Player"))
            {
                // Try two perpendicular directions and pick the first clear one
                Vector2 perp = Vector2.Perpendicular(direction).normalized;
                Vector2 tryDir1 = (direction + perp * 0.6f).normalized;
                Vector2 tryDir2 = (direction - perp * 0.6f).normalized;

                bool clear1 = Physics2D.Raycast(rb.position, tryDir1, debugRayDistance).collider == null;
                bool clear2 = Physics2D.Raycast(rb.position, tryDir2, debugRayDistance).collider == null;

                if (debugDraw)
                {
                    Debug.DrawRay(rb.position, tryDir1 * debugRayDistance, clear1 ? Color.cyan : Color.magenta, 0.1f);
                    Debug.DrawRay(rb.position, tryDir2 * debugRayDistance, clear2 ? Color.cyan : Color.magenta, 0.1f);
                }

                if (clear1)
                    direction = tryDir1;
                else if (clear2)
                    direction = tryDir2;
                // else keep original direction and let physics attempt to resolve collision
            }

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
        // draw attack and detection spheres at their configured offset centers
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector3)transform.position + (Vector3)attackRangeOffset, attackRange);
        // mark the center
        Gizmos.DrawWireSphere((Vector3)transform.position + (Vector3)attackRangeOffset, 0.05f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector3)transform.position + (Vector3)detectionRangeOffset, detectionRange);
        Gizmos.DrawWireSphere((Vector3)transform.position + (Vector3)detectionRangeOffset, 0.05f);
    }

    // ensure any leftover UI is removed if this object is destroyed by other systems
    private void OnDestroy()
    {
        if (hpBarInstance != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(hpBarInstance);
#else
            Destroy(hpBarInstance);
#endif
            hpBarInstance = null;
            greenBar = null;
            redBar = null;
        }
    }
}