using UnityEngine;

public class BehaviorAgentHandler : MonoBehaviour
{
    public GameObject Target { get; private set; }

    // Called when the player enters the detection range
    public void SetTarget(Transform playerTransform)
    {
        if (playerTransform != null)
        {
            Target = playerTransform.gameObject;
            Debug.Log("Target set to: " + Target.name);
        }
        else
        {
            Debug.LogWarning("SetTarget called with a null playerTransform.");
        }
    }

    // Called when the player exits the detection range
    public void ClearTarget()
    {
        Target = null;
        Debug.Log("Target cleared.");
    }
}
