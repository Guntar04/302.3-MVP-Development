using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores all the data about our dungeon.
/// Useful when creating a save / load system
/// </summary>
public class DungeonData : MonoBehaviour
{
    public GameObject PlayerReference;
    public List<Room> Rooms = new List<Room>();
    public HashSet<Vector2Int> Path = new HashSet<Vector2Int>();

    public void Reset()
    {
        // Clear stored room/path data
        Rooms?.Clear();
        Path?.Clear();

        // If there is a runtime player instance, destroy it safely depending on play/edit mode
        if (PlayerReference != null)
        {
            if (Application.isPlaying)
                Destroy(PlayerReference);
            else
                DestroyImmediate(PlayerReference);
            PlayerReference = null;
        }

        // Do NOT Destroy(this.gameObject) here — the generator/editor code expects DungeonData to exist.
    }

    public IEnumerator TutorialCoroutine(Action code)
    {
        yield return new WaitForSeconds(1);
        code();
    }
}


/// <summary>
/// Holds all the data about the room
/// </summary>
public class Room
{
    public Vector2 RoomCenterPos { get; set; }
    public HashSet<Vector2Int> FloorTiles { get; private set; } = new HashSet<Vector2Int>();

    public HashSet<Vector2Int> NearWallTilesUp { get; set; } = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> NearWallTilesDown { get; set; } = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> NearWallTilesLeft { get; set; } = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> NearWallTilesRight { get; set; } = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> CornerTiles { get; set; } = new HashSet<Vector2Int>();

    public HashSet<Vector2Int> InnerTiles { get; set; } = new HashSet<Vector2Int>();

    public HashSet<Vector2Int> PropPositions { get; set; } = new HashSet<Vector2Int>();
    public List<GameObject> PropObjectReferences { get; set; } = new List<GameObject>();

    public List<Vector2Int> PositionsAccessibleFromPath { get; set; } = new List<Vector2Int>();

    public List<GameObject> EnemiesInTheRoom { get; set; } = new List<GameObject>();

    public Room(Vector2 roomCenterPos, HashSet<Vector2Int> floorTiles)
    {
        this.RoomCenterPos = roomCenterPos;
        this.FloorTiles = floorTiles;
    }
}
