using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Required for UI components like Image

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private Animator animator;
    public int health = 10;
    public Slider healthSlider;
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

    [Header("UI Settings")]
    public GameObject criticalHitUIPrefab;
    public float criticalHitUIDuration = 1f; // Duration to display the UI



    private Vector2 moveDirection;
    private Vector2 lastMoveDirection = Vector2.down; // default facing down
    public Image dashIconOverlay;

    private bool canDash = true;
    private bool isDashing = false;
    private bool isAttacking = false;
    private bool isInvincible = false; // Tracks if the player is invincible
    private Coroutine flashCoroutine;

    private void Start()
    {
        // Dynamically assign the DashIconOverlay from the UIManager
        if (UIManager.Instance != null)
        {
            dashIconOverlay = UIManager.Instance.dashIconOverlay;
            // Try to get the health slider from the UIManager if available.
            if (healthSlider == null && UIManager.Instance.healthSlider != null)
            {
                healthSlider = UIManager.Instance.healthSlider;
                // initialize slider values
                healthSlider.maxValue = maxHealth;
                healthSlider.value = health;
            }
        }
        else
        {
            Debug.LogError("UIManager is not present in the scene.");
        }

        // Initialize the dash icon overlay to show the ability is not ready
        if (dashIconOverlay != null)
        {
            dashIconOverlay.fillAmount = 0f; // Fully filled (ability not ready)
        }
    }

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
        Debug.Log($"Number of enemies detected: {hitEnemies.Length}");

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

                    // Display critical hit UI
                    if (criticalHitUIPrefab != null)
                    {
                        var canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            var criticalHitUIInstance = Instantiate(criticalHitUIPrefab, canvas.transform);
                            var rectTransform = criticalHitUIInstance.GetComponent<RectTransform>();

                            if (rectTransform != null)
                            {
                                Vector3 worldPosition = transform.position + new Vector3(0, 1, 0); // Offset by 1 unit vertically
                                rectTransform.position = Camera.main.WorldToScreenPoint(worldPosition); // Convert world position to screen position
                                Destroy(criticalHitUIInstance, criticalHitUIDuration); // Automatically destroy the UI after the duration
                            }
                            else
                            {
                                Debug.LogError("Critical Hit UI prefab does not have a RectTransform component.");
                            }
                        }
                        else
                        {
                            Debug.LogError("No Canvas found in the scene.");
                        }
                    }
                    else
                    {
                        Debug.LogError("CriticalHitUIPrefab is not assigned in the Inspector.");
                    }
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

        // Perform the dash
        while (Time.time < dashEndTime)
        {
            transform.Translate(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
        isInvincible = false; // Disable invincibility after dash

        // Start cooldown visual
        StartCoroutine(DashCooldownVisual());

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // Cooldown visual logic
    private IEnumerator DashCooldownVisual()
    {
        float elapsedTime = 0f;

        while (elapsedTime < dashCooldown)
        {
            elapsedTime += Time.deltaTime;
            float fillAmount = 1f - (elapsedTime / dashCooldown); // Calculate fill amount
            dashIconOverlay.fillAmount = fillAmount; // Update overlay fill amount
            yield return null;
        }

        dashIconOverlay.fillAmount = 0f; // Reset overlay when cooldown is complete
    }

    public void TakeDamage(int damage)
    {
        // First check shield component (blocks one attack if available)
        var shield = GetComponent<Shield>();
        if (shield != null && shield.TryConsumeShield())
        {
            Debug.Log("PlayerShield: blocked incoming damage.");
            // Optional: play shield block VFX/sound/animation here
            return; // damage blocked, do not reduce HP
        }

        if (isInvincible) return; // Prevent damage if the player is invincible

        health -= damage;

        Debug.Log($"Player Health: {health}");
        if (health <= 0)
        {
            health = 0;
            Debug.Log("Player has died.");
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white; // Reset color to default
            }
            if (animator != null)
            {
                animator.Play("Death");
            }
            else
            {
                Debug.LogError("Animator is not assigned to PlayerController.");
            }
            // Add death logic here (e.g., trigger game over screen)
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine); // Stop flashing if the player dies
            }
        }
        else
        {
            Debug.Log($"Player took {damage} damage. Remaining health: {health}");
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashRedOnHit());
        }
        UpdatePlayerHealth();
    }

    private IEnumerator FlashRedOnHit()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red; // Change to red to indicate damage
            yield return new WaitForSeconds(0.1f); // Flash duration
            spriteRenderer.color = originalColor; // Revert to original color
        }
        else
        {
            Debug.LogError("SpriteRenderer is not assigned to PlayerController.");
        }
    }

    public void UpdatePlayerHealth()
    {
        // Update the assigned UI slider (if present). This method is called
        // whenever health changes (e.g. TakeDamage) so the UI reflects current HP.
        if (healthSlider != null)
        {
            // ensure slider ranges are correct
            healthSlider.maxValue = maxHealth;
            healthSlider.value = Mathf.Clamp(health, 0, maxHealth);
        }
        else
        {
            // fallback: try to find a Slider named "HealthUI" in the scene (non-Editor-safe)
            var go = GameObject.Find("HealthUI");
            if (go != null)
            {
                var s = go.GetComponent<UnityEngine.UI.Slider>();
                if (s != null)
                {
                    healthSlider = s;
                    healthSlider.maxValue = maxHealth;
                    healthSlider.value = Mathf.Clamp(health, 0, maxHealth);
                }
            }
        }
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