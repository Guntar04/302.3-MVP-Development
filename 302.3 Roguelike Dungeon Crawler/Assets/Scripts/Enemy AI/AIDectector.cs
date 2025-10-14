using UnityEngine;

public class AIDetection : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public GameObject player;
    public float speed = 2f;
    public float detectionRange = 4f;

    private Vector2 lastPosition;
    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
        lastPosition = transform.position;
    }

    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);
        Vector2 direction = player.transform.position - transform.position;
        direction.Normalize();

        // Move toward player if within range
        if (distance < detectionRange)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
        }

        UpdateAnimation(direction);
        lastPosition = transform.position;
    }

    private void UpdateAnimation(Vector2 direction)
    {
        Vector2 velocity = (Vector2)transform.position - lastPosition;

        // Check if the AI is actually moving
        if (velocity.magnitude < 0.01f)
        {
            animator.Play("Idle");
        }
        else
        {
            // Flip sprite / facing direction
            if (direction.x < 0)
                transform.localScale = new Vector3(-baseScale.x, baseScale.y, baseScale.z);
            else if (direction.x > 0)
                transform.localScale = new Vector3(baseScale.x, baseScale.y, baseScale.z);

            animator.Play("Run");
        }
    }
}
