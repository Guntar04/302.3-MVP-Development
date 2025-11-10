using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private const int MAX_HEALTH_CAP = 20;

    [Header("Stats")]
    [SerializeField] private Animator animator;
    public int health = 10;
    public Slider healthSlider;
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
public float maxSpeed = 3f; // inspector sets this

    [Header("Combat Speed")]
public float baseAttackCooldown = 1f; // default attack delay
private float attackSpeedMultiplier = 1f; // affected by equipment


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
private EquipmentStats equippedWeaponStats;
private EquipmentStats equippedChestplateStats;
private EquipmentStats equippedHelmetStats;
private EquipmentStats equippedPantsStats;
private EquipmentStats equippedBootsStats;
private EquipmentStats equippedShieldStats;

    public static bool PlayerDead { get; private set; } = false;

    public static void SetPlayerDead(bool value) => PlayerDead = value;

    private void Start()
    {
        maxHealth = Mathf.Clamp(maxHealth, 1, MAX_HEALTH_CAP);

        if (UIManager.Instance != null)
        {
            dashIconOverlay = UIManager.Instance.dashIconOverlay;
            if (healthSlider == null && UIManager.Instance.healthSlider != null)
            {
                healthSlider = UIManager.Instance.healthSlider;
                healthSlider.maxValue = maxHealth;
                health = Mathf.Clamp(health, 0, maxHealth);
                healthSlider.value = health;
            }
        }

        if (PlayerProgress.HasSaved)
        {
            PlayerProgress.ApplyTo(this);
            UpdatePlayerHealth();
        }

        if (dashIconOverlay != null) dashIconOverlay.fillAmount = 0f;

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
        if (!isDashing) Move();
    }

    private void HandleMovementInput()
    {
        float moveX = 0f, moveY = 0f;
        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;

        moveDirection = new Vector2(moveX, moveY).normalized;
        if (moveDirection != Vector2.zero) lastMoveDirection = moveDirection;

        if (!isAttacking)
        {
            if (moveDirection == Vector2.zero) animator.Play("Idle");
            else if (moveY > 0) animator.Play("Run_Up");
            else if (moveY < 0) animator.Play("Run_Down");
            else if (moveX > 0) animator.Play("Run_Right");
            else if (moveX < 0) animator.Play("Run_Left");
        }
    }

  public IEnumerator PlayerAttack()
    {
        if (isAttacking) yield break;
        isAttacking = true;

        // Attack animation and hit detection code
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;

        string attackAnim = "Attack_Down";
        Vector2 attackDir = Vector2.zero;

        if (Mathf.Abs(directionToMouse.x) > Mathf.Abs(directionToMouse.y))
        {
            attackAnim = directionToMouse.x > 0 ? "Attack_Right" : "Attack_Left";
            attackDir = directionToMouse.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            attackAnim = directionToMouse.y > 0 ? "Attack_Up" : "Attack_Down";
            attackDir = directionToMouse.y > 0 ? Vector2.up : Vector2.down;
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
                bool isCriticalHit = Random.value < criticalHitChance;
                int damage = isCriticalHit ? attackDamage * 2 : attackDamage;
                ai.TakeDamage(damage);

                if (isCriticalHit && criticalHitUIPrefab != null)
                {
                    var canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        var critUI = Instantiate(criticalHitUIPrefab, canvas.transform);
                        critUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up);
                        Destroy(critUI, criticalHitUIDuration);
                    }
                }
            }
        }

float animationTime = 0.3f; // or however long your attack animation lasts

// Make cooldown = max(animationTime, adjusted speed)
float cooldown = Mathf.Max(animationTime, baseAttackCooldown / attackSpeedMultiplier);
yield return new WaitForSeconds(cooldown);

        isAttacking = false;
    }



    private void Move() => transform.Translate(moveDirection * moveSpeed * Time.fixedDeltaTime);

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
            dashIconOverlay.fillAmount = 1f - (elapsedTime / dashCooldown);
            yield return null;
        }
        dashIconOverlay.fillAmount = 0f;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        health -= damage;
        if (health <= 0 && !isDead)
        {
            health = 0;
            isDead = true;
            PlayerDead = true;
            StartCoroutine(HandleDeathAndLoad());
        }
        UpdatePlayerHealth();
    }

    private IEnumerator HandleDeathAndLoad()
    {
        animator?.Play("Death");
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Death")) yield return null;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f) yield return null;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        if (animator != null) animator.enabled = false;
        PlayerDead = false;
        SceneManager.LoadScene("LoseMenu");
    }

    public void Heal(int amount)
    {
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        UpdatePlayerHealth();
    }

    public void UpdatePlayerHealth()
    {
        health = Mathf.Clamp(health, 0, Mathf.Min(maxHealth, MAX_HEALTH_CAP));
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }
    }

    // ---- EQUIP/UNEQUIP ----
public void EquipItemStats(EquipmentStats stats, Loot.EquipmentType type)
{
    if (stats == null) return;

    switch (type)
    {
case Loot.EquipmentType.Sword:
    equippedWeaponStats = stats;
    attackDamage = baseAttack + stats.attackPower;
    attackSpeedMultiplier = stats.attackSpeed; // assume attackSpeed is % increase
    Debug.Log($"Equipped Sword → added attack={stats.attackPower}");
    Debug.Log($"Equipped Sword → attackDamage={attackDamage}, attackSpeedMultiplier={attackSpeedMultiplier}");
    break;

        case Loot.EquipmentType.Chestplate:
            equippedChestplateStats = stats;
            defense = baseDefense + stats.defense;
            Debug.Log($"Equipped Chestplate → defense={defense}");
            break;

        case Loot.EquipmentType.Helmet:
            equippedHelmetStats = stats;
            defense = baseDefense + stats.defense;
            Debug.Log($"Equipped Helmet → defense={defense}");
            break;

        case Loot.EquipmentType.Pants:
            equippedPantsStats = stats;
            defense = baseDefense + stats.defense;
            Debug.Log($"Equipped Pants → defense={defense}");
            break;

        case Loot.EquipmentType.Boots:
            equippedBootsStats = stats;
            defense = baseDefense + stats.defense;
            moveSpeed = baseMoveSpeed + stats.moveSpeed;
            Debug.Log($"Equipped Boots → defense={defense}, moveSpeed={moveSpeed}");
            break;

        case Loot.EquipmentType.Shield:
            equippedShieldStats = stats;
            defense = baseDefense + stats.defense;
            Debug.Log($"Equipped Shield → defense={defense}");
            break;
    }
}



public void UnequipItemStats(Loot.EquipmentType type)
{
    switch (type)
    {
case Loot.EquipmentType.Sword:
    equippedWeaponStats = null;
    attackDamage = baseAttack;
    attackSpeedMultiplier = 1f;
    break;


        case Loot.EquipmentType.Chestplate:
            equippedChestplateStats = null;
            defense = baseDefense;
            break;

        case Loot.EquipmentType.Helmet:
            equippedHelmetStats = null;
            defense = baseDefense;
            break;

        case Loot.EquipmentType.Pants:
            equippedPantsStats = null;
            defense = baseDefense;
            break;

        case Loot.EquipmentType.Boots:
            equippedBootsStats = null;
            defense = baseDefense;
            moveSpeed = baseMoveSpeed;
            break;

        case Loot.EquipmentType.Shield:
            equippedShieldStats = null;
            defense = baseDefense;
            break;
    }
}


}
