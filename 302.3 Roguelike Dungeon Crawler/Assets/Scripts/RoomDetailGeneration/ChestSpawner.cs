using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ChestSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private int minChests = 1;
    [SerializeField] private int maxChests = 2;
    [SerializeField] private bool autoSpawnOnStart = true;
    [SerializeField] private float waitTimeout = 2f;

    [Tooltip("Radius used to detect existing objects when choosing a spawn position")]
    public float spawnCheckRadius = 0.25f;
    [Tooltip("Number of attempts to find a nearby free position")]
    public int spawnPositionAttempts = 12;
    [Tooltip("Max distance (world units) to search around the desired tile")]
    public float spawnSearchRadius = 1f;

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

    private bool IsPositionFree(Vector2 pos)
    {
        // check colliders overlapping the spot
        var hits = Physics2D.OverlapCircleAll(pos, spawnCheckRadius);
        foreach (var h in hits)
        {
            if (h == null) continue;
            // check known components that represent blocking/important objects
            if (h.GetComponent<Chest>() != null) return false;
            if (h.GetComponent<ExitTrigger>() != null) return false;
            if (h.GetComponent<AIController>() != null) return false;
            if (h.GetComponent<LootPickup>() != null) return false;
            if (h.GetComponent<ExitKeyPickup>() != null) return false;
            if (h.GetComponent<HealthPot>() != null) return false;
            // you can add other checks (tags/layers) as needed
        }
        return true;
    }

    private bool FindFreeSpawnPosition(Vector2 origin, out Vector2 result)
    {
        if (IsPositionFree(origin))
        {
            result = origin;
            return true;
        }

        // try several random offsets in a circle around the origin
        for (int i = 0; i < spawnPositionAttempts; i++)
        {
            float angle = (i / (float)spawnPositionAttempts) * Mathf.PI * 2f;
            float r = Random.Range(0f, spawnSearchRadius);
            Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            if (IsPositionFree(candidate))
            {
                result = candidate;
                return true;
            }
        }

        result = origin;
        return false;
    }

    // Example usage inside your existing spawn method:
    private void SpawnChestAt(Vector2 spawnPos)
    {
        if (!FindFreeSpawnPosition(spawnPos, out Vector2 freePos))
        {
            Debug.Log($"ChestSpawner: no free position found near {spawnPos}, skipping chest spawn.");
            return;
        }

        Instantiate(chestPrefab, (Vector3)freePos, Quaternion.identity);
    }
}
