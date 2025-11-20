using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    private const int MAX_HEALTH_CAP = 20;

    // Make this public so other scripts can fire the event
    public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)

    // Add this new method after the event declaration
    public void SetHealth(int newHealth, int newMaxHealth)
    {
        maxHealth = Mathf.Clamp(newMaxHealth, 1, MAX_HEALTH_CAP);
        health = Mathf.Clamp(newHealth, 0, maxHealth);
        
        // Fire the health changed event
        OnHealthChanged?.Invoke(health, maxHealth);
        
        Debug.Log($"PlayerController.SetHealth: Set to {health}/{maxHealth}");
    }

    [Header("Stats")]
    [SerializeField] private Animator animator;
    public int health = 10;
    public int maxHealth = 10;
    public int attackDamage = 2;
    public int defense = 1;

    [Header("Base Stats")]
    public int baseAttack = 2;
    public float baseMoveSpeed = 5f;
    public int baseDefense = 1;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float dashSpeed = 10f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Attack Settings")]
    public float attackRange = 1f;
    public float attackRadius = 0.6f;
    public LayerMask enemyLayer;

    [Header("Critical Hit Settings")]
    public float criticalHitChance = 0.2f;

    [Header("UI Settings")]
    public GameObject criticalHitUIPrefab;
    public float criticalHitUIDuration = 1f;

    [Header("Equipment stat ranges")]
    public float minSpeed = 0f;
    public float maxSpeed = 3f;

    private Vector2 moveDirection;
    private Vector2 lastMoveDirection = Vector2.down;
    public Image dashIconOverlay;

    private bool canDash = true;
    private bool isDashing = false;
    private bool isAttacking = false;
    private bool isInvincible = false;
    private bool isDead = false;
    private Coroutine flashCoroutine;
    private Coroutine deathCoroutine;
    private Color spriteOriginalColor = Color.white;

    // Equipped stats
    public EquipmentStats equippedWeaponStats;
    public EquipmentStats equippedChestplateStats;
    public EquipmentStats equippedHelmetStats;
    public EquipmentStats equippedPantsStats;
    public EquipmentStats equippedBootsStats;
    public EquipmentStats equippedShieldStats;

    public static bool PlayerDead { get; private set; } = false;

    public static void SetPlayerDead(bool value) => PlayerDead = value;

    public static class PlayerDeathInfo
    {
        public static string EnemyName;
        public static Sprite EnemySprite;
    }

    private void Start()
    {
        // enforce cap on configured maxHealth
        maxHealth = Mathf.Clamp(maxHealth, 1, MAX_HEALTH_CAP);

        // Apply saved progress (if any)
        if (PlayerProgress.HasSaved)
        {
            PlayerProgress.ApplyTo(this);
        }

        // cache original sprite color
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) spriteOriginalColor = sr.color;

        // Tell UIManager to bind to this player
        if (UIManager.Instance != null)
        {
            UIManager.Instance.BindPlayer(gameObject);
            Debug.Log($"PlayerController.Start: Requested UIManager bind for instanceID {GetInstanceID()}");
        }

        // Get dash icon reference
        if (UIManager.Instance != null && dashIconOverlay == null)
        {
            dashIconOverlay = UIManager.Instance.dashIconOverlay;
            if (dashIconOverlay != null)
                dashIconOverlay.fillAmount = 0f;
        }

        // Fire initial health event after UIManager has time to bind
        StartCoroutine(FireInitialHealthEvent());
    }

    private IEnumerator FireInitialHealthEvent()
    {
        // Wait for UIManager to finish binding (3 frames)
        yield return null;
        yield return null;
        yield return null;

        // NOW fire the initial health change event
        OnHealthChanged?.Invoke(health, maxHealth);
        Debug.Log($"PlayerController: Fired initial OnHealthChanged - {health}/{maxHealth}");
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

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;

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

        animator.Play(attackAnim);

        Vector2 attackOrigin = (Vector2)transform.position + attackDir * attackRange * 0.5f;
        Vector2 attackSize = attackDir.x != 0
            ? new Vector2(attackRange, attackRadius)
            : new Vector2(attackRadius, attackRange);

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackOrigin, attackSize, 0f, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            AIController ai = enemy.GetComponent<AIController>();
            if (ai != null)
            {
                bool isCriticalHit = UnityEngine.Random.value < criticalHitChance;
                int damage = isCriticalHit ? attackDamage * 2 : attackDamage;

                ai.TakeDamage(damage);

                if (isCriticalHit)
                {
                    Debug.Log("Critical Hit! Damage: " + damage);

                    if (criticalHitUIPrefab != null)
                    {
                        var canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            var criticalHitUIInstance = Instantiate(criticalHitUIPrefab, canvas.transform);
                            var rectTransform = criticalHitUIInstance.GetComponent<RectTransform>();

                            if (rectTransform != null)
                            {
                                Vector3 worldPosition = transform.position + new Vector3(0, 1, 0);
                                rectTransform.position = Camera.main.WorldToScreenPoint(worldPosition);
                                Destroy(criticalHitUIInstance, criticalHitUIDuration);
                            }
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.3f);
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
        isInvincible = true;

        Vector2 dashDirection = moveDirection == Vector2.zero ? lastMoveDirection : moveDirection;
        float dashEndTime = Time.time + dashDuration;

        while (Time.time < dashEndTime)
        {
            transform.Translate(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
        isInvincible = false;

        StartCoroutine(DashCooldownVisual());

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private IEnumerator DashCooldownVisual()
    {
        float elapsedTime = 0f;

        while (elapsedTime < dashCooldown)
        {
            elapsedTime += Time.deltaTime;
            float fillAmount = 1f - (elapsedTime / dashCooldown);
            if (dashIconOverlay != null)
                dashIconOverlay.fillAmount = fillAmount;
            yield return null;
        }

        if (dashIconOverlay != null)
            dashIconOverlay.fillAmount = 0f;
    }

    public void TakeDamage(int damage, AIController killer = null)
    {
        if (isDead || isInvincible) return;

        var shield = GetComponent<Shield>();
        if (shield != null && shield.TryConsumeShield())
        {
            Debug.Log("PlayerShield: blocked incoming damage.");
            return;
        }

        health -= damage;
        if (health < 0) health = 0;

        Debug.Log($"Player took {damage} damage. Remaining health: {health}");

        OnHealthChanged?.Invoke(health, maxHealth);

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashRedOnHit());

        if (health <= 0)
        {
            Die(killer);
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        health += amount;
        if (health > maxHealth) health = maxHealth;

        Debug.Log($"Player healed {amount}. Current health: {health}/{maxHealth}");

        OnHealthChanged?.Invoke(health, maxHealth);
    }

    private void Die(AIController killer = null)
    {
        health = 0;
        isDead = true;
        PlayerDead = true;
        Debug.Log("Player has died.");
        isInvincible = true;

        var col2d = GetComponent<Collider2D>();
        if (col2d != null) col2d.enabled = false;
        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.simulated = false;

        try { gameObject.tag = "Untagged"; } catch { }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = spriteOriginalColor;
        }

        if (animator != null)
        {
            if (deathCoroutine != null) StopCoroutine(deathCoroutine);
            deathCoroutine = StartCoroutine(HandleDeathAndLoad(killer));
        }
        else
        {
            Debug.LogError("Animator is not assigned. Loading LoseMenu immediately.");
            SceneManager.LoadScene("LoseMenu");
        }
    }

    private IEnumerator HandleDeathAndLoad(AIController killer = null)
    {
        animator.Play("Death");

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Death"))
            yield return null;

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        if (animator != null) animator.enabled = false;

        if (killer != null)
        {
            GameData.EnemyName = killer.enemyName;
            GameData.EnemySprite = killer.enemySprite;
        }
        else
        {
            GameData.EnemyName = "Unknown";
            GameData.EnemySprite = null;
        }

        yield return new WaitForSeconds(0.15f);

        PlayerDead = false;

        SceneManager.LoadScene("LoseMenu");
    }

    private IEnumerator FlashRedOnHit()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = spriteOriginalColor;
        }
    }

    private void ApplyStats(EquipmentStats stats, Loot.EquipmentType type)
    {
        if (stats == null) return;

        switch (type)
        {
            case Loot.EquipmentType.Sword:
                attackDamage = baseAttack + stats.attackPower;
                break;
            case Loot.EquipmentType.Chestplate:
            case Loot.EquipmentType.Helmet:
            case Loot.EquipmentType.Shield:
            case Loot.EquipmentType.Pants:
            case Loot.EquipmentType.Boots:
                defense += stats.defense;
                break;
        }
    }

    public void EquipItemStats(EquipmentStats stats, Loot.EquipmentType type)
    {
        switch (type)
        {
            case Loot.EquipmentType.Sword:
                equippedWeaponStats = stats;
                break;
            case Loot.EquipmentType.Chestplate:
                equippedChestplateStats = stats;
                break;
            case Loot.EquipmentType.Helmet:
                equippedHelmetStats = stats;
                break;
            case Loot.EquipmentType.Pants:
                equippedPantsStats = stats;
                break;
            case Loot.EquipmentType.Boots:
                equippedBootsStats = stats;
                break;
            case Loot.EquipmentType.Shield:
                equippedShieldStats = stats;
                break;
        }

        UpdatePlayerStats();
    }

    public void UnequipItemStats(Loot.EquipmentType type)
    {
        Debug.Log($"UnequipItemStats CALLED → {type}");

        switch (type)
        {
            case Loot.EquipmentType.Sword:
                if (equippedWeaponStats != null)
                {
                    attackDamage -= equippedWeaponStats.attackPower;
                    equippedWeaponStats = null;
                }
                break;
            case Loot.EquipmentType.Chestplate:
                if (equippedChestplateStats != null)
                {
                    defense -= equippedChestplateStats.defense;
                    equippedChestplateStats = null;
                }
                break;
            case Loot.EquipmentType.Helmet:
                equippedHelmetStats = null;
                break;
            case Loot.EquipmentType.Pants:
                equippedPantsStats = null;
                break;
            case Loot.EquipmentType.Boots:
                equippedBootsStats = null;
                break;
            case Loot.EquipmentType.Shield:
                equippedShieldStats = null;
                break;
        }

        UpdatePlayerStats();
    }

    public void UpdatePlayerStats()
    {
        Debug.Log("=== UPDATE PLAYER STATS START ===");

        attackDamage = baseAttack;
        defense = baseDefense;

        if (equippedWeaponStats != null) ApplyStats(equippedWeaponStats, Loot.EquipmentType.Sword);
        if (equippedChestplateStats != null) ApplyStats(equippedChestplateStats, Loot.EquipmentType.Chestplate);
        if (equippedHelmetStats != null) ApplyStats(equippedHelmetStats, Loot.EquipmentType.Helmet);
        if (equippedPantsStats != null) ApplyStats(equippedPantsStats, Loot.EquipmentType.Pants);
        if (equippedBootsStats != null) ApplyStats(equippedBootsStats, Loot.EquipmentType.Boots);
        if (equippedShieldStats != null) ApplyStats(equippedShieldStats, Loot.EquipmentType.Shield);

        Debug.Log($"FINAL STATS → Attack={attackDamage}, Defense={defense}, MoveSpeed={moveSpeed}");
        Debug.Log("=== UPDATE PLAYER STATS END ===");
    }

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


