using UnityEngine;

public class AIDetection : MonoBehaviour
{
    [SerializeField] public bool PlayerInArea { get; private set; }
    public Transform Player { get; private set; }

    [SerializeField] private string detectionTag = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("OnTriggerEnter2D called with: " + collision.name);

        if (collision.CompareTag(detectionTag))
        {
            PlayerInArea = true;
            Player = collision.transform;

            Debug.Log("Player detected: " + Player.name);

            // Notify the behavior graph about the detected player
            SendMessage("SetTarget", Player, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(detectionTag))
        {
            PlayerInArea = false;
            Player = null;

            Debug.Log("Player exited detection range.");

            // Clear the target in the behavior graph
            SendMessage("ClearTarget", SendMessageOptions.DontRequireReceiver);
        }
    }
}