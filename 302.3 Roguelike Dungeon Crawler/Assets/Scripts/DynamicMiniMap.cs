using UnityEngine;

public class DynamicMiniMap : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 20;
    public int mapHeight = 20;
    public Transform miniMapParent;
    public GameObject tilePrefab;
    public GameObject playerIconPrefab;
    public float tileSize = 10f;

    private GameObject[,] miniMapTiles;
    private bool[,] discovered;
    private GameObject playerIcon;

    [Header("Player")]
    public Transform playerTransform; // Assign your player in Inspector

    private int[,] mapGrid; // 0 = floor, 1 = wall

    void Start()
    {
        GenerateMap();
        CreateMiniMap();
        CreatePlayerIcon();
    }

    void Update()
    {
        UpdatePlayerIcon();
        RevealTilesAroundPlayer();
    }

    void GenerateMap()
    {
        mapGrid = new int[mapWidth, mapHeight];
        discovered = new bool[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Example procedural generation: random walls
                mapGrid[x, y] = Random.value < 0.2f ? 1 : 0;
                discovered[x, y] = false; // nothing discovered at start
            }
        }
    }

    void CreateMiniMap()
    {
        miniMapTiles = new GameObject[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                GameObject tile = Instantiate(tilePrefab, miniMapParent);
                tile.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);
                tile.GetComponent<SpriteRenderer>().color = Color.black; // start hidden
                miniMapTiles[x, y] = tile;
            }
        }
    }

    void CreatePlayerIcon()
    {
        playerIcon = Instantiate(playerIconPrefab, miniMapParent);
    }

    void UpdatePlayerIcon()
    {
        Vector3 playerPos = playerTransform.position;
        playerIcon.transform.localPosition = new Vector3(playerPos.x * tileSize, playerPos.y * tileSize, 0);
    }

    void RevealTilesAroundPlayer()
    {
        Vector3 playerPos = playerTransform.position;
        int px = Mathf.RoundToInt(playerPos.x);
        int py = Mathf.RoundToInt(playerPos.y);

        int revealRadius = 2; // tiles around the player

        for (int x = px - revealRadius; x <= px + revealRadius; x++)
        {
            for (int y = py - revealRadius; y <= py + revealRadius; y++)
            {
                if (x >= 0 && y >= 0 && x < mapWidth && y < mapHeight)
                {
                    discovered[x, y] = true;
                    miniMapTiles[x, y].GetComponent<SpriteRenderer>().color = mapGrid[x, y] == 1 ? Color.gray : Color.white;
                }
            }
        }
    }
}
