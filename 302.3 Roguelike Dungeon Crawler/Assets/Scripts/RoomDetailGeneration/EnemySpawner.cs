using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int minPerRoom = 3;
    [SerializeField] private int maxPerRoom = 6;
    [SerializeField] private bool skipPlayerRoom = true;

    private DungeonData dungeonData;
    private HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    private void Awake()
    {
        dungeonData = Object.FindFirstObjectByType<DungeonData>();
    }

    public void PlaceEnemies()
    {
        if (dungeonData == null || dungeonData.Rooms == null || dungeonData.Rooms.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: No dungeon data/rooms available.");
            return;
        }
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: enemyPrefab not assigned.");
            return;
        }

        // determine player's room index (if present)
        int playerRoomIndex = -1;
        if (dungeonData.PlayerReference != null)
        {
            Vector2 playerPos = dungeonData.PlayerReference.transform.position;
            Vector2Int playerCell = new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y));
            for (int i = 0; i < dungeonData.Rooms.Count; i++)
            {
                if (dungeonData.Rooms[i].FloorTiles.Contains(playerCell))
                {
                    playerRoomIndex = i;
                    break;
                }
            }
        }

        Transform parent = new GameObject("SpawnedEnemies").transform;
        parent.SetParent(transform, false);

        for (int i = 0; i < dungeonData.Rooms.Count; i++)
        {
            if (skipPlayerRoom && i == playerRoomIndex) continue;

            var room = dungeonData.Rooms[i];
            if (room == null || room.FloorTiles == null || room.FloorTiles.Count == 0) continue;

            int count = Random.Range(minPerRoom, maxPerRoom + 1);
            var candidates = room.FloorTiles.Where(p => !dungeonData.Path.Contains(p) && !occupied.Contains(p)).ToList();
            if (candidates.Count == 0) continue;

            for (int n = 0; n < count; n++)
            {
                if (candidates.Count == 0) break;
                var chosen = candidates[Random.Range(0, candidates.Count)];
                occupied.Add(chosen);
                candidates.Remove(chosen);

                Vector3 world = new Vector3(chosen.x + 0.5f, chosen.y + 0.5f, 0f);
                Instantiate(enemyPrefab, world, Quaternion.identity, parent);
            }
        }
    }
}
