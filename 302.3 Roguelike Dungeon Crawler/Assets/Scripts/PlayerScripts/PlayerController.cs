using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private Animator animator;
    public int health = 10;
    public int maxHealth = 10;
    public int attackDamage = 2;
    public int defense = 1;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float dashSpeed = 10f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Attack Settings")]
    public float attackRange = 1f;
    public float attackRadius = 0.6f;
    public LayerMask enemyLayer; // Assign your enemy layer in Inspector

    [Header("Critical Hit Settings")]
    public float criticalHitChance = 0.2f; // 20% chance for a critical hit

    private Vector2 moveDirection;
    private Vector2 lastMoveDirection = Vector2.down; // default facing down

    private bool canDash = true;
    private bool isDashing = false;
    private bool isAttacking = false;
    private bool isInvincible = false; // Tracks if the player is invincible

    private void Update()
    {
        if (!isDashing)
        {
            HandleMovementInput();

            if (Input.GetKeyDown(KeyCode.Space))
                StartCoroutine(Dash());

            if (Input.GetMouseButtonDown(0) && !isAttacking)
                StartCoroutine(PlayerAttack());
        }
    }

    private void FixedUpdate()
    {
        if (!isDashing)
            Move();
    }

    private void HandleMovementInput()
    {
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;

        moveDirection = new Vector2(moveX, moveY).normalized;

        if (moveDirection != Vector2.zero)
            lastMoveDirection = moveDirection;

        if (!isAttacking)
        {
            if (moveDirection == Vector2.zero)
            {
                animator.Play("Idle");
            }
            else
            {
                if (moveY > 0)
                    animator.Play("Run_Up");
                else if (moveY < 0)
                    animator.Play("Run_Down");
                else if (moveX > 0)
                    animator.Play("Run_Right");
                else if (moveX < 0)
                    animator.Play("Run_Left");
            }
        }
    }

    private IEnumerator PlayerAttack()
    {
        isAttacking = true;

        // Get mouse position in world space
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;

        // Determine attack animation and direction
        string attackAnim = "Attack_Down";
        Vector2 attackDir = Vector2.zero;

        if (Mathf.Abs(directionToMouse.x) > Mathf.Abs(directionToMouse.y))
        {
            if (directionToMouse.x > 0)
            {
                attackAnim = "Attack_Right";
                attackDir = Vector2.right;
            }
            else
            {
                attackAnim = "Attack_Left";
                attackDir = Vector2.left;
            }
        }
        else
        {
            if (directionToMouse.y > 0)
            {
                attackAnim = "Attack_Up";
                attackDir = Vector2.up;
            }
            else
            {
                attackAnim = "Attack_Down";
                attackDir = Vector2.down;
            }
        }

        // Play animation
        animator.Play(attackAnim);

        // Calculate attack hitbox
        Vector2 attackOrigin = (Vector2)transform.position + attackDir * attackRange * 0.5f;
        Vector2 attackSize = attackDir.x != 0
            ? new Vector2(attackRange, attackRadius)  // horizontal attack
            : new Vector2(attackRadius, attackRange); // vertical attack

        // Detect enemies in hitbox
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackOrigin, attackSize, 0f, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            AIController ai = enemy.GetComponent<AIController>();
            if (ai != null)
            {
                // Determine if the attack is a critical hit
                bool isCriticalHit = Random.value < criticalHitChance;
                int damage = isCriticalHit ? attackDamage * 2 : attackDamage;

                ai.TakeDamage(damage);

                if (isCriticalHit)
                {
                    Debug.Log("Critical Hit! Damage: " + damage);
                }
            }
        }

        yield return new WaitForSeconds(0.3f); // adjust to match animation length
        isAttacking = false;
    }

    private void Move()
    {
        transform.Translate(moveDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private IEnumerator Dash()
    {
        if (!canDash) yield break;

        canDash = false;
        isDashing = true;
        isInvincible = true; // Enable invincibility during dash

        Vector2 dashDirection = moveDirection == Vector2.zero ? lastMoveDirection : moveDirection;
        float dashEndTime = Time.time + dashDuration;

        while (Time.time < dashEndTime)
        {
            transform.Translate(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
        isInvincible = false; // Disable invincibility after dash

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return; // Prevent damage if the player is invincible

        health -= damage;

        if (health <= 0)
        {
            health = 0;
            Debug.Log("Player has died.");
            if (animator != null)
            {
                animator.Play("Death");
            }
            else
            {
                Debug.LogError("Animator is not assigned to PlayerController.");
            }
            // Add death logic here (e.g., trigger game over screen)
        }
        else
        {
            Debug.Log($"Player took {damage} damage. Remaining health: {health}");
            StartCoroutine(FlashRedOnHit());
        }
        UpdatePlayerHealth();
    }

    private IEnumerator FlashRedOnHit()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f); // Flash duration
            spriteRenderer.color = originalColor;
        }
        else
        {
            Debug.LogError("SpriteRenderer is not assigned to PlayerController.");
        }
    }

    public void UpdatePlayerHealth()
    {
        //Add in the updated when Olivia has made the UI for it
    }

    // Draw attack hitbox in Scene view for debugging
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorldPos - transform.position).normalized;
        Vector2 attackPoint = (Vector2)transform.position + dir * attackRange * 0.5f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint, new Vector3(
            dir.x != 0 ? attackRange : attackRadius,
            dir.y != 0 ? attackRange : attackRadius,
            0f));
    }
}