using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer))]
public class Chest : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite closedSprite;
    public Sprite openSprite;

    [Header("Interaction")]
    public KeyCode openKey = KeyCode.E;
    public string playerTag = "Player";
    public UnityEvent OnOpened;

    private SpriteRenderer sr;
    private bool playerInRange = false;
    private bool isOpen = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (closedSprite != null) sr.sprite = closedSprite;
    }

    void Update()
    {
        if (isOpen) return;
        if (playerInRange && Input.GetKeyDown(openKey))
        {
            Open();
        }
    }

    private void Open()
    {
        isOpen = true;
        if (openSprite != null) sr.sprite = openSprite;
        // disable interaction collider so it cannot be opened again
        var colliders = GetComponents<Collider2D>();
        foreach (var c in colliders) c.enabled = false;
        OnOpened?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) playerInRange = false;
    }
}
