using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChestSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private int minChests = 1;
    [SerializeField] private int maxChests = 2;
    [SerializeField] private bool autoSpawnOnStart = true;
    [SerializeField] private float waitTimeout = 2f;

    private DungeonData dd;

    private void Start()
    {
        if (autoSpawnOnStart)
            StartCoroutine(WaitAndSpawnCoroutine(waitTimeout));
    }

    public void SpawnChests()
    {
        dd = FindFirstObjectByType<DungeonData>();
        if (dd == null || dd.Rooms == null || dd.Rooms.Count == 0)
        {
            Debug.LogWarning("ChestSpawner: No dungeon rooms available to place chests.");
            return;
        }

        if (chestPrefab == null)
        {
            Debug.LogWarning("ChestSpawner: chestPrefab not assigned.");
            return;
        }

        // determine player room (if player exists) to avoid placing chest there
        int playerRoomIndex = -1;
        if (dd.PlayerReference != null)
        {
            Vector2 playerPos = dd.PlayerReference.transform.position;
            Vector2Int playerCell = new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y));
            for (int i = 0; i < dd.Rooms.Count; i++)
            {
                if (dd.Rooms[i].FloorTiles.Contains(playerCell)) { playerRoomIndex = i; break; }
            }
        }

        // eligible rooms (non-empty floor and not player room)
        var eligible = new List<int>();
        for (int i = 0; i < dd.Rooms.Count; i++)
            if (dd.Rooms[i].FloorTiles != null && dd.Rooms[i].FloorTiles.Count > 0 && i != playerRoomIndex)
                eligible.Add(i);

        if (eligible.Count == 0)
        {
            Debug.LogWarning("ChestSpawner: No eligible rooms to spawn chests.");
            return;
        }

        int count = Mathf.Clamp(Random.Range(minChests, maxChests + 1), 0, eligible.Count);

        var parent = new GameObject("Chests").transform;
        parent.SetParent(transform, false);

        var chosenRoomIndices = new HashSet<int>();
        for (int n = 0; n < count; n++)
        {
            // pick a room not already chosen
            int ri;
            if (eligible.Count == chosenRoomIndices.Count) break;
            do
            {
                ri = eligible[Random.Range(0, eligible.Count)];
            } while (chosenRoomIndices.Contains(ri));
            chosenRoomIndices.Add(ri);

            var room = dd.Rooms[ri];
            var floorList = room.FloorTiles.ToList();
            if (floorList.Count == 0) continue;

            var tile = floorList[Random.Range(0, floorList.Count)];
            Vector3 world = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
            var inst = Instantiate(chestPrefab, world, Quaternion.identity, parent);
            // ensure chest has Chest component
            if (inst.GetComponent<Chest>() == null)
            {
                Debug.LogWarning("ChestSpawner: chestPrefab missing Chest component.");
            }
        }
    }

    private IEnumerator WaitAndSpawnCoroutine(float timeout)
    {
        float t = 0f;
        dd = FindFirstObjectByType<DungeonData>();
        while (t < timeout)
        {
            dd = FindFirstObjectByType<DungeonData>();
            if (dd != null && dd.Rooms != null && dd.Rooms.Count > 0) break;
            t += Time.deltaTime;
            yield return null;
        }

        if (dd == null || dd.Rooms == null || dd.Rooms.Count == 0)
        {
            Debug.LogWarning("ChestSpawner: No rooms found after wait; not spawning chests.");
            yield break;
        }

        SpawnChests();
    }
}
