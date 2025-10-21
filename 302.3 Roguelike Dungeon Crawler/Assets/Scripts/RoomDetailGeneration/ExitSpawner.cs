using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ExitSpawner : MonoBehaviour
{
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private int exitRoomIndex = -1; // -1 => auto (rightmost / opposite to player)

    private DungeonData dungeonData;

    private void Awake()
    {
        dungeonData = FindFirstObjectByType<DungeonData>();
    }

    public void SpawnExit()
    {
        if (dungeonData == null || dungeonData.Rooms == null || dungeonData.Rooms.Count == 0 || exitPrefab == null)
        {
            Debug.LogWarning("ExitSpawner: missing data/prefab.");
            return;
        }

        int index = exitRoomIndex;
        if (index < 0 || index >= dungeonData.Rooms.Count)
        {
            // pick rightmost room by computed center
            float maxX = float.MinValue;
            for (int i = 0; i < dungeonData.Rooms.Count; i++)
            {
                var r = dungeonData.Rooms[i];
                var c = GetRoomCenter(r);
                if (c.x > maxX)
                {
                    maxX = c.x;
                    index = i;
                }
            }
        }

        var room = dungeonData.Rooms[index];
        if (room == null || room.FloorTiles == null || room.FloorTiles.Count == 0) return;

        var chosen = room.FloorTiles.ToList()[UnityEngine.Random.Range(0, room.FloorTiles.Count)];
        Vector3 world = new Vector3(chosen.x + 0.5f, chosen.y + 0.5f, 0f);
        var inst = Instantiate(exitPrefab, world, Quaternion.identity, transform);
        var trigger = inst.GetComponent<ExitTrigger>();
        if (trigger == null) trigger = inst.AddComponent<ExitTrigger>();
        trigger.OnRequestNextFloor.AddListener(() => Debug.Log("ExitSpawner: Next floor requested (hook Scene change here)."));
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

    public float spawnCheckRadius = 0.25f;
    public int spawnPositionAttempts = 12;
    public float spawnSearchRadius = 1f;

    private bool IsPositionFree(Vector2 pos)
    {
        var hits = Physics2D.OverlapCircleAll(pos, spawnCheckRadius);
        foreach (var h in hits)
        {
            if (h == null) continue;
            if (h.GetComponent<Chest>() != null) return false;
            if (h.GetComponent<ExitTrigger>() != null) return false;
            if (h.GetComponent<AIController>() != null) return false;
            if (h.GetComponent<LootPickup>() != null) return false;
            if (h.GetComponent<ExitKeyPickup>() != null) return false;
            if (h.GetComponent<HealthPot>() != null) return false;
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
    private void SpawnExitAt(Vector2 spawnPos)
    {
        if (!FindFreeSpawnPosition(spawnPos, out Vector2 freePos))
        {
            Debug.Log($"ExitSpawner: no free position found near {spawnPos}, skipping exit spawn.");
            return;
        }

        Instantiate(exitPrefab, (Vector3)freePos, Quaternion.identity);
    }
}
