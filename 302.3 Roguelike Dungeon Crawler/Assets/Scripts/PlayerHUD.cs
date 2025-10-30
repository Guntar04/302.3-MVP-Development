using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHUD : MonoBehaviour
{
    [Header("Health Settings")]
    public PlayerStats playerStats;
    public GameObject healthChunkPrefab; // prefab = single chunk
    public Transform healthBarContainer;
    public Sprite fullChunkSprite, threeQuarterSprite, halfSprite, quarterSprite, emptySprite;

    [Header("Shield Settings")]
    public GameObject shieldBarPrefab; // prefab = single bar container
    public Transform shieldBarContainer;
    public List<Sprite> shieldChunkSprites; // index 0 = 1 chunk, index 4 = 5 chunks (full bar)

    [Header("Layout Settings")]
    public int chunksPerBar = 4; // max chunks per bar
    public float barSpacing = 10f; // vertical spacing between bars

    private List<GameObject> healthChunks = new List<GameObject>();
    private List<GameObject> shieldBars = new List<GameObject>();

    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats reference missing from PlayerHUD!");
            return;
        }

        // Initialize health and shield
        playerStats.currentHealth = playerStats.GetMaxHealth();
        playerStats.currentShield = playerStats.GetMaxShield();

        PlayerStats.OnStatsChanged += UpdateHUD;

        BuildHealthBar();
        BuildShieldBars();
        UpdateHealth();
        UpdateShield();
    }

    private void OnDestroy()
    {
        PlayerStats.OnStatsChanged -= UpdateHUD;
    }

    // -----------------------------
    // Health
    // -----------------------------
    private void BuildHealthBar()
    {
        foreach (Transform child in healthBarContainer)
            Destroy(child.gameObject);
        healthChunks.Clear();

        int totalChunks = Mathf.CeilToInt(playerStats.GetMaxHealth() / 25f);
        for (int i = 0; i < totalChunks; i++)
        {
            GameObject chunk = Instantiate(healthChunkPrefab, healthBarContainer);
            healthChunks.Add(chunk);
        }
    }

    private void UpdateHealth()
    {
        int totalHealth = playerStats.currentHealth;

        for (int i = 0; i < healthChunks.Count; i++)
        {
            int remaining = Mathf.Clamp(totalHealth - i * 25, 0, 25);
            Image img = healthChunks[i].GetComponent<Image>();
            if (img == null) continue;

            if (remaining == 25) img.sprite = fullChunkSprite;
            else if (remaining >= 19) img.sprite = threeQuarterSprite;
            else if (remaining >= 13) img.sprite = halfSprite;
            else if (remaining >= 7) img.sprite = quarterSprite;
            else img.sprite = emptySprite;
        }
    }

    // -----------------------------
    // Shield
    // -----------------------------
    private void BuildShieldBars()
    {
        foreach (Transform child in shieldBarContainer)
            Destroy(child.gameObject);
        shieldBars.Clear();

        int totalShield = playerStats.currentShield;
        int barsNeeded = Mathf.CeilToInt((float)totalShield / (chunksPerBar * 25));

        for (int i = 0; i < barsNeeded; i++)
        {
            CreateShieldBar(i, totalShield);
        }
    }

    private void CreateShieldBar(int index, int totalShield)
    {
        GameObject bar = Instantiate(shieldBarPrefab, shieldBarContainer);
        bar.transform.localPosition = new Vector3(0, -index * barSpacing, 0);
        shieldBars.Add(bar);

        Image barImage = bar.GetComponent<Image>();
        if (barImage == null) return;

        int remainingShield = Mathf.Clamp(totalShield - index * chunksPerBar * 25, 0, chunksPerBar * 25);
        int chunksInBar = Mathf.CeilToInt((float)remainingShield / 25f);
        chunksInBar = Mathf.Clamp(chunksInBar, 0, shieldChunkSprites.Count);

        // Indexing: 0 = 1 chunk, 4 = full bar
        barImage.sprite = (chunksInBar > 0) ? shieldChunkSprites[chunksInBar - 1] : shieldChunkSprites[0];
    }

    private void UpdateShield()
    {
        int totalShield = playerStats.currentShield;
        int barsNeeded = Mathf.CeilToInt((float)totalShield / (chunksPerBar * 25));

        // Add new bars if needed
        while (shieldBars.Count < barsNeeded)
        {
            CreateShieldBar(shieldBars.Count, totalShield);
        }

        // Remove excess bars if shield decreased
        while (shieldBars.Count > barsNeeded)
        {
            Destroy(shieldBars[shieldBars.Count - 1]);
            shieldBars.RemoveAt(shieldBars.Count - 1);
        }

        // Update each bar sprite based on current shield
        for (int i = 0; i < shieldBars.Count; i++)
        {
            Image barImage = shieldBars[i].GetComponent<Image>();
            if (barImage == null) continue;

            int remainingShield = Mathf.Clamp(totalShield - i * chunksPerBar * 25, 0, chunksPerBar * 25);
            int chunksInBar = Mathf.CeilToInt((float)remainingShield / 25f);
            chunksInBar = Mathf.Clamp(chunksInBar, 0, shieldChunkSprites.Count);

            barImage.sprite = (chunksInBar > 0) ? shieldChunkSprites[chunksInBar - 1] : shieldChunkSprites[0];
        }
    }

    // -----------------------------
    // Public update
    // -----------------------------
    public void UpdateHUD()
    {
        BuildHealthBar();
        BuildShieldBars();
        UpdateHealth();
        UpdateShield();
    }
}
