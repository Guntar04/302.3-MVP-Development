using System.Linq;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int playerRoomIndex = -1; // -1 = auto (leftmost)
    [SerializeField] private bool autoPlaceOnStart = true;

    private DungeonData dungeonData;

    private void Start()
    {
        // keep Start minimal — defer placement to PlacePlayer which will acquire fresh DungeonData
        if (autoPlaceOnStart)
            StartCoroutine(WaitAndPlacePlayerCoroutine(2f));
    }

    // public method used by generator or other managers
    public void PlacePlayer()
    {
        // always resolve DungeonData at call time (avoid stale null captured at Start)
        dungeonData = FindFirstObjectByType<DungeonData>();
        if (dungeonData == null || dungeonData.Rooms == null || dungeonData.Rooms.Count == 0)
        {
            Debug.LogWarning("PlayerSpawner: No dungeon data/rooms available.");
            return;
        }

        // skip if a player already exists (prevents duplicates)
        if (dungeonData.PlayerReference != null)
        {
            Debug.Log("PlayerSpawner: Player already exists, skipping PlacePlayer().");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogWarning("PlayerSpawner: playerPrefab not assigned.");
            return;
        }

        int index = playerRoomIndex;
        if (index < 0 || index >= dungeonData.Rooms.Count)
        {
            // compute leftmost room by averaging floor tile positions
            float minX = float.MaxValue;
            for (int i = 0; i < dungeonData.Rooms.Count; i++)
            {
                var c = GetRoomCenter(dungeonData.Rooms[i]);
                if (c.x < minX)
                {
                    minX = c.x;
                    index = i;
                }
            }
        }

        var room = dungeonData.Rooms[index];
        if (room == null || room.FloorTiles == null || room.FloorTiles.Count == 0)
        {
            Debug.LogWarning("PlayerSpawner: chosen room has no floor tiles.");
            return;
        }

        var candidates = room.FloorTiles.ToList();
        var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        Vector3 world = new Vector3(chosen.x + 0.5f, chosen.y + 0.5f, 0f);
        var p = Instantiate(playerPrefab, world, Quaternion.identity);
        dungeonData.PlayerReference = p;

        // optionally immediately assign camera if present
        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.player = p.transform;
    }

    public void SpawnPlayer()
    {
        // Safety: destroy any existing player first
        var existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in existingPlayers)
        {
            Debug.Log($"PlayerSpawner: Destroying existing player '{p.name}' before spawn");
            Destroy(p);
        }

        // Now spawn the new player
        PlacePlayer();
    }

    private System.Collections.IEnumerator WaitAndPlacePlayerCoroutine(float timeout)
    {
        float t = 0f;
        while (t < timeout)
        {
            dungeonData = FindFirstObjectByType<DungeonData>();
            if (dungeonData != null && dungeonData.Rooms != null && dungeonData.Rooms.Count > 0)
                break;
            t += Time.deltaTime;
            yield return null;
        }

        dungeonData = dungeonData ?? FindFirstObjectByType<DungeonData>();
        if (dungeonData == null || dungeonData.Rooms == null || dungeonData.Rooms.Count == 0)
        {
            Debug.LogWarning("PlayerSpawner: No dungeon data/rooms available after wait.");
            yield break;
        }

        PlacePlayer();
    }

    private Vector2 GetRoomCenter(Room room)
    {
        if (room == null || room.FloorTiles == null || room.FloorTiles.Count == 0) return Vector2.zero;
        float sx = 0, sy = 0;
        foreach (var t in room.FloorTiles)
        {
            sx += t.x;
            sy += t.y;
        }
        float cnt = room.FloorTiles.Count;
        return new Vector2(sx / cnt, sy / cnt);
    }
}
