using UnityEngine;

public class ShieldController : MonoBehaviour
{
    public float maxShield = 100f;
    public float currentShield = 100;
    public SpriteStatBar shieldBar;
    public float shieldDamage = 20f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) // example key
        {
            TakeDamage(shieldDamage);
        }
        if (Input.GetKeyDown(KeyCode.L)) // test restore
        {
            RestoreShield(15f);
        }
    }

    void Start()
    {
        currentShield = maxShield;
        if (shieldBar != null) shieldBar.SetFromCurrentMax(currentShield, maxShield);
    }

    public void TakeDamage(float amount)
    {
         currentShield = Mathf.Clamp(currentShield - amount, 0f, maxShield);
        if (shieldBar != null) shieldBar.SetFromCurrentMax(currentShield, maxShield);
        
    }

    public void RestoreShield(float amount)
    {
         currentShield = Mathf.Clamp(currentShield + amount, 0f, maxShield);
        if (shieldBar != null) shieldBar.SetFromCurrentMax(currentShield, maxShield);
    
    }

    
}
