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

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private int currentHealth;
    private Transform player;
    private Rigidbody2D rb;
    private bool isAttacking = false;
    private float attackTimer = 0f;

    private Vector3 baseScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
    }

    void Start()
    {
        currentHealth = maxHealth;
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (player == null) return;

        attackTimer -= Time.deltaTime;

        float distance = Vector2.Distance(transform.position, player.position);

        // Attack if in range, cooldown ready, and not attacking
        if (distance <= attackRange && attackTimer <= 0f && !isAttacking)
        {
            StartCoroutine(Attack());
            attackTimer = attackCooldown;
        }

        UpdateAnimation();
        FlipSprite();
    }

    private IEnumerator Attack()
    {
        isAttacking = true;

        // only try to play an attack animation if it exists
        if (animator != null)
        {
            int layer = 0;
            int hash = Animator.StringToHash("Attack");
            if (animator.HasState(layer, hash))
                animator.Play("Attack", layer);
        }

        // Deal damage to player
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.health -= attackPower;
            Debug.Log("Enemy attacked player for " + attackPower + " damage. Player health: " + playerController.health);
        }

        // Wait for attack duration
        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        // choose the state name you expect in your Animator Controller
        string stateName = "Idle";
        if (isAttacking)
        {
            // no Attack animation in your controller: fall back to Idle while "attacking"
            stateName = "Idle";
        }
        else if (rb != null && rb.linearVelocity.sqrMagnitude > 0.001f)
        {
            stateName = "Run";
        }

        int layer = 0;
        int hash = Animator.StringToHash(stateName);

        // Only try to play the state if it exists on the animator (silently do nothing otherwise)
        if (animator.HasState(layer, hash))
        {
            animator.Play(stateName, layer);
        }
        // else: do nothing (avoids repeated warnings)
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Move only if not attacking and within detection range but outside attack range
        if (!isAttacking && distance <= detectionRange && distance > attackRange)
        {
            Vector2 direction = ((Vector2)player.position - rb.position).normalized;
            // use linearVelocity (non-obsolete)
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Stop completely during attack or out of range
        }
    }

    private void FlipSprite()
    {
        if (player == null) return;

        if (player.position.x < transform.position.x)
            transform.localScale = new Vector3(-baseScale.x, baseScale.y, baseScale.z);
        else
            transform.localScale = baseScale;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    public bool IsAttacking() => isAttacking;
}