using UnityEngine;

public class AICombat : MonoBehaviour
{
    [SerializeField] int attackPower = 2;
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float attackCooldown = 1.0f;
    [SerializeField] int maxHealth = 10;
    int currentHealth;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
