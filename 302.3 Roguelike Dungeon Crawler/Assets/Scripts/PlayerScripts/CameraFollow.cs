using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        if (player != null)
        {
            transform.position = player.position + new Vector3(0, 0, -10); // Adjust the offset as needed
        }
    }
}
