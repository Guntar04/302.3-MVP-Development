using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Required for UI components like Image
using UnityEngine.SceneManagement; // <-- added for scene loading

public class PlayerController : MonoBehaviour
{
    private const int MAX_HEALTH_CAP = 20; // global hard cap

    [Header("Stats")]
    [SerializeField] private Animator animator;
    public int health = 10;
    public Slider healthSlider;
    public int maxHealth = 10; // will be clamped to MAX_HEALTH_CAP in Start()
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
    private bool isDead = false; // prevent input after death
    private Coroutine flashCoroutine;
    private Coroutine deathCoroutine;
    private Color spriteOriginalColor = Color.white; // store original color so flash restores reliably

    // Global flag enemies can check
    public static bool PlayerDead { get; private set; } = false;

    // allow external code (main menu / restart flow) to clear the flag
    public static void SetPlayerDead(bool value)
    {
        PlayerDead = value;
    }

    private void Start()
    {
        // enforce cap on configured maxHealth
        maxHealth = Mathf.Clamp(maxHealth, 1, MAX_HEALTH_CAP);

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
                // make sure current health respects cap
                health = Mathf.Clamp(health, 0, maxHealth);
                healthSlider.value = health;
            }
        }
        else
        {
            Debug.LogError("UIManager is not present in the scene.");
        }

        // Apply saved progress (if any) so spawned player receives previous HP/shields
        if (PlayerProgress.HasSaved)
        {
            PlayerProgress.ApplyTo(this);
            // ensure slider and UI reflect applied values
            UpdatePlayerHealth();
        }

        // Initialize the dash icon overlay to show the ability is not ready
        if (dashIconOverlay != null)
        {
            dashIconOverlay.fillAmount = 0f; // Fully filled (ability not ready)
        }

        // cache original sprite color so flashes always restore to the correct value
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) spriteOriginalColor = sr.color;
    }

    private void Update()
    {
        if (isDead) return;
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
        if (isDead) return;
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
        //Debug.Log($"Number of enemies detected: {hitEnemies.Length}");

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
        if (health <= 0 && !isDead)
        {
            health = 0;
            isDead = true;
            PlayerDead = true;                 // notify others
            Debug.Log("Player has died.");
            isInvincible = true;               // prevent further damage

            // disable collisions/physics so enemies stop detecting and hitting the player
            var col2d = GetComponent<Collider2D>();
            if (col2d != null) col2d.enabled = false;
            var rb2d = GetComponent<Rigidbody2D>();
            if (rb2d != null) rb2d.simulated = false;

            // remove the "Player" tag so tag-based targeting won't find it
            try { gameObject.tag = "Untagged"; } catch { }
            // stop any hit flash and restore original color
if (flashCoroutine != null)
{
    StopCoroutine(flashCoroutine);
    flashCoroutine = null;
    var sr = GetComponent<SpriteRenderer>();
    if (sr != null) sr.color = spriteOriginalColor;
}
// optionally disable other player components that drive targeting/input here
// e.g. GetComponent<PlayerController>().enabled = false; (don't disable this script if it controls death coroutine)
// play death and start coroutine that waits for animation end then loads LoseMenu
if (animator != null)
{
    // start the death coroutine (keeps waiting for animation to finish)
    if (deathCoroutine != null) StopCoroutine(deathCoroutine);
    deathCoroutine = StartCoroutine(HandleDeathAndLoad());
}
else
{
    Debug.LogError("Animator is not assigned to PlayerController. Loading LoseMenu immediately.");
    SceneManager.LoadScene("LoseMenu");
}
return;
        }
        else
        {
            Debug.Log($"Player took {damage} damage. Remaining health: {health}");
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = spriteOriginalColor;
            }
            flashCoroutine = StartCoroutine(FlashRedOnHit());
        }
        UpdatePlayerHealth();
    }
    
    private IEnumerator HandleDeathAndLoad()
    {
        // play the death animation
        animator.Play("Death");

        // wait until the animator is actually in the Death state
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Death"))
            yield return null;

        // wait for the death animation to finish (normalizedTime >= 1)
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        // Animation finished — immediately hide the player so the animation won't replay
        // Disable SpriteRenderer and the Animator to prevent further state changes
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        if (animator != null) animator.enabled = false;

        // small buffer then load lose menu
        yield return new WaitForSeconds(0.15f);

        // reset static flag (scene change will unload but keep defensive)
        PlayerDead = false;

        SceneManager.LoadScene("LoseMenu");
    }

    // Public heal method that other scripts (potions) should call.
    // This guarantees health never exceeds maxHealth and the global cap.
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        UpdatePlayerHealth();
    }

    private IEnumerator FlashRedOnHit()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // set to red, wait, then restore cached original color
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = spriteOriginalColor;
            flashCoroutine = null;
        }
        else
        {
            Debug.LogError("SpriteRenderer is not assigned to PlayerController.");
        }
    }

    public void UpdatePlayerHealth()
    {
        // Enforce global caps before updating UI
        // clamp health to valid range (ensures external direct writes get corrected)
        health = Mathf.Clamp(health, 0, Mathf.Min(maxHealth, MAX_HEALTH_CAP));

        if (healthSlider != null)
        {
            // ensure slider ranges are correct
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
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
                    healthSlider.value = health;
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