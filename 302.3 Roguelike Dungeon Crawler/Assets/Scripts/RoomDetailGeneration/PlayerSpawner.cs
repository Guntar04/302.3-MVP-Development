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
        StartCoroutine(WaitAndPlacePlayer());
    }

    private System.Collections.IEnumerator WaitAndPlacePlayer()
    {
        float timeout = 2f; // seconds to wait for the generator
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

        if (autoPlaceOnStart) PlacePlayer();
    }

    public void PlacePlayer()
    {
        // skip if generator or another spawner already placed a player
        var dd = FindFirstObjectByType<DungeonData>();
        if (dd != null && dd.PlayerReference != null)
        {
            Debug.Log("PlayerSpawner: Player already exists, skipping PlacePlayer().");
            return;
        }

        if (dungeonData == null || dungeonData.Rooms == null || dungeonData.Rooms.Count == 0)
        {
            Debug.LogWarning("PlayerSpawner: No dungeon data/rooms available.");
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
            // pick leftmost room by computing each room's center from its floor tiles
            float minX = float.MaxValue;
            for (int i = 0; i < dungeonData.Rooms.Count; i++)
            {
                var c = GetRoomCenter(dungeonData.Rooms[i]);
                if (c.x < minX) { minX = c.x; index = i; }
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
        // return center in grid coordinates (no +0.5, used only for comparisons)
        return new Vector2(sx / cnt, sy / cnt);
    }
}
