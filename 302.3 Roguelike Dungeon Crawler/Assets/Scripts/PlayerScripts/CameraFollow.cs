using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    void Start()
    {
        // try to get a valid runtime player (will clear prefab references)
        TryAssignPlayerReference();
    }

    void LateUpdate()
    {
        // keep trying until a real player instance is found
        if (player == null)
            TryAssignPlayerReference();

        if (player != null)
            transform.position = player.position + new Vector3(0, 0, -10);
    }

    private void TryAssignPlayerReference()
    {
        // If inspector field points to a prefab (not in a loaded scene), treat it as null
        if (player != null)
        {
            var go = player.gameObject;
            if (!go.scene.IsValid()) // prefab or asset, not a scene instance
            {
                player = null;
            }
        }

        if (player != null) return;

        // Prefer DungeonData's runtime reference (set by your spawner)
        var dd = UnityEngine.Object.FindAnyObjectByType<DungeonData>();
        if (dd != null && dd.PlayerReference != null)
        {
            player = dd.PlayerReference.transform;
            return;
        }

        // Fallback: find by tag (ensure player prefab is tagged "Player")
        var pgo = GameObject.FindWithTag("Player");
        if (pgo != null) player = pgo.transform;
    }
}
