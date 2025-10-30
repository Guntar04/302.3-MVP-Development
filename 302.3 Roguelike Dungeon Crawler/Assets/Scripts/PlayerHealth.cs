using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Link to UI")]
    public SpriteStatBar healthBar;

    [Header("Testing")]
    public float damagePerHit = 15f;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null) healthBar.SetFromCurrentMax(currentHealth, maxHealth);
    }

    void Update()
    {
        // quick test: press H to take damage, J to heal a bit
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(damagePerHit);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            Heal(10f);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        if (healthBar != null) healthBar.SetFromCurrentMax(currentHealth, maxHealth);

        // TODO: play damage sound, flash screen, trigger animations etc.
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        if (healthBar != null) healthBar.SetFromCurrentMax(currentHealth, maxHealth);
    }
}
