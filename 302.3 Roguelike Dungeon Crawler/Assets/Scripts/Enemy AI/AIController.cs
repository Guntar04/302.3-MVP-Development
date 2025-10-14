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

    void FixedUpdate()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Move only if not attacking and within detection range but outside attack range
        if (!isAttacking && distance <= detectionRange && distance > attackRange)
        {
            Vector2 direction = ((Vector2)player.position - rb.position).normalized;
            rb.linearVelocity = direction * moveSpeed;  // Updated for Unity 2025+
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Stop completely during attack or out of range
        }
    }

    private IEnumerator Attack()
    {
        isAttacking = true;

        // Optional: play attack animation
        if (animator != null)
        {
            animator.Play("Attack");
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

        if (isAttacking)
        {
            // animator.Play("Attack");
        }
        else if (rb.linearVelocity.magnitude > 0.01f)
        {
            animator.Play("Run");
        }
        else
        {
            animator.Play("Idle");
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