using System.Linq;
using UnityEngine;

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
}
