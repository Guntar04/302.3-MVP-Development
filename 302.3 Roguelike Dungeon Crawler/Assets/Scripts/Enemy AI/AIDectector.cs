using UnityEngine;

public class AIDetection : MonoBehaviour
{
    [SerializeField] public bool PlayerInArea { get; private set; }
    public Transform Player { get; private set; }

    [SerializeField] private string detectionTag = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(detectionTag))
        {
            PlayerInArea = true;
            Player = collision.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(detectionTag))
        {
            PlayerInArea = false;
            Player = null;
        }
    }
}
