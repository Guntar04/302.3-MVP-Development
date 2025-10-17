using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class AIController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 10;
    public int attackPower = 1;

    [Header("Combat")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float attackDuration = 0.5f;

    [Header("Detection & Movement")]
    public float detectionRange = 4f;
    public float moveSpeed = 2f;

    [Header("Stun / Death")]
    [Tooltip("Seconds stunned after being hit")]
    public float hitStunDuration = 2f;
    [Tooltip("Fallback seconds to wait before destroying dead enemy if no animation length can be determined")]
    [SerializeField] private float destroyDelay = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Drops")]
    [Tooltip("Prefab to spawn when this enemy dies (optional)")]
    public GameObject healthPotPrefab;
    [Range(0f, 1f)]
    [Tooltip("Chance (0..1) that a health pot will drop on death")]
    public float healthDropChance = 0.333f;

    private int currentHealth;
    private Transform player;
    private Rigidbody2D rb;
    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDead = false;
    private float attackTimer = 0f;

    private Vector3 baseScale;
    // Animator parameter names (change here if you rename in the controller)
    private static readonly int ParamIsMoving = Animator.StringToHash("isMoving");
    private static readonly int ParamHit = Animator.StringToHash("Hit");
    private static readonly int ParamAttack = Animator.StringToHash("Attack");
    private static readonly int ParamDeath = Animator.StringToHash("Death");

    private bool shouldMoveFlag = false;

    // add: avoid re-triggering hit every frame
    private bool hitTriggered = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
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
            // ensure animator reflects idle
            shouldMoveFlag = false;
            UpdateAnimationParameters();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        // movement intent: chase when within detection but outside attack range
        shouldMoveFlag = !isDead && !isStunned && !isAttacking && distance <= detectionRange && distance > attackRange;

        if (!isStunned && !isAttacking && distance <= attackRange && attackTimer <= 0f)
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

        // remove continuous trigger here — only trigger Hit once when damage happens
        // if (isStunned) animator.SetTrigger(ParamHit);
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

        if (animator != null) animator.SetTrigger(ParamAttack);

        if (player != null)
        {
            var playerController = player.GetComponent<PlayerController>();
            if (playerController != null) playerController.health -= attackPower;
        }

        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth > 0)
        {
            // only trigger hit animation once per hit/stun
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
            // force Play Death instantly so we can wait for its end
            int deathStateHash = Animator.StringToHash("Death");
            animator.Play(deathStateHash, 0, 0f);
        }

        // removed immediate spawn here — drop will occur after death animation finishes

        // wait for death animation to finish then destroy (use configured destroyDelay)
        StartCoroutine(WaitForDeathAndDestroy(destroyDelay));
    }
    private IEnumerator WaitForDeathAndDestroy(float fallback)
    {
        if (animator == null)
        {
            // If no animator, spawn drop immediately then destroy after fallback
            if (healthPotPrefab != null && Random.value <= healthDropChance)
                Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

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

            Destroy(gameObject);
            yield break;
        }

        // wait until the Death state's playback has reached its end (normalizedTime >= 1)
        while (true)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.shortNameHash == deathHash && info.normalizedTime >= 1f)
            {
                // spawn drop at enemy position immediately before destroying
                if (healthPotPrefab != null && Random.value <= healthDropChance)
                    Instantiate(healthPotPrefab, transform.position, Quaternion.identity);

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
}