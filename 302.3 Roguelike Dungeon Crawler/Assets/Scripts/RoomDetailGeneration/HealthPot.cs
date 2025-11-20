using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class HealthPot : MonoBehaviour
{
    //Amount of health restored when picked up
    public int healAmount = 2;

    //Tag used to identify the player
    public string playerTag = "Player";

    public UnityEvent OnPickedUp;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Heal the player using the Heal() method
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            Debug.Log($"HealthPot picked up, healing player by {healAmount}");
            
            // Use the Heal method which handles clamping and fires OnHealthChanged event
            pc.Heal(healAmount);
            
            Debug.Log($"Player health is now {pc.health}/{pc.maxHealth}");
        }

        OnPickedUp?.Invoke();
        Destroy(gameObject);
    }
}
