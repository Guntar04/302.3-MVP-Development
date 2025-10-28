using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHUD : MonoBehaviour
{
    [Header("Health Settings")]
    public PlayerStats playerStats;
    public GameObject healthBarPrefab;
    public Transform healthBarContainer;
    public Sprite fullChunkSprite, threeQuarterSprite, halfSprite, quarterSprite, emptySprite;

    [Header("Shield Settings")]
    public GameObject shieldBarPrefab;
    public Transform shieldBarContainer;
    public Sprite shieldFullSprite, shieldThreeQuarterSprite, shieldHalfSprite, shieldQuarterSprite, shieldEmptySprite;

    [Header("Layout Settings")]
    public int maxChunksPerRow = 4;
    public float chunkSpacing = 30f;
    public float rowSpacing = 20f;

    private List<Image> healthChunks = new List<Image>();
    private List<Image> shieldChunks = new List<Image>();

    private int lastMaxHealth = -1;
    private int lastMaxShield = -1;

    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats reference missing from PlayerHUD!");
            return;
        }

        // Initialize stats to full if starting new
        playerStats.currentHealth = playerStats.GetMaxHealth();
        playerStats.currentShield = playerStats.GetMaxShield();

        PlayerStats.OnStatsChanged += UpdateHUD;

        BuildBars();
        UpdateHUD();
    }

    private void OnDestroy()
    {
        PlayerStats.OnStatsChanged -= UpdateHUD;
    }

    private void BuildBars()
    {
        BuildHealthBars();
        BuildShieldBars();
    }

    private void BuildHealthBars()
    {
        foreach (Transform child in healthBarContainer)
            Destroy(child.gameObject);
        healthChunks.Clear();

        int totalHealthChunks = Mathf.CeilToInt(playerStats.GetMaxHealth() / 25f);

        for (int i = 0; i < totalHealthChunks; i++)
        {
            GameObject chunk = Instantiate(healthBarPrefab, healthBarContainer);
            Image img = chunk.GetComponent<Image>() ?? chunk.GetComponentInChildren<Image>();
            healthChunks.Add(img);
        }

        lastMaxHealth = playerStats.GetMaxHealth();
    }

    private void BuildShieldBars()
    {
        foreach (Transform child in shieldBarContainer)
            Destroy(child.gameObject);
        shieldChunks.Clear();

        int totalShieldChunks = Mathf.CeilToInt(playerStats.GetMaxShield() / 25f);

        for (int i = 0; i < totalShieldChunks; i++)
        {
            GameObject chunk = Instantiate(shieldBarPrefab, shieldBarContainer);
            Image img = chunk.GetComponent<Image>() ?? chunk.GetComponentInChildren<Image>();
            shieldChunks.Add(img);
        }

        lastMaxShield = playerStats.GetMaxShield();
    }

    public void UpdateHUD()
    {
        if (playerStats == null) return;

        // Rebuild only if max health/shield changed
        if (playerStats.GetMaxHealth() != lastMaxHealth)
            BuildHealthBars();

        if (playerStats.GetMaxShield() != lastMaxShield)
            BuildShieldBars();

        UpdateHealth();
        UpdateShield();
    }

    private void UpdateHealth()
    {
        int totalHealth = playerStats.currentHealth;
        int maxHealth = playerStats.GetMaxHealth();
        int totalChunks = Mathf.CeilToInt(maxHealth / 25f);

        for (int i = 0; i < totalChunks; i++)
        {
            int row = i / maxChunksPerRow;
            int column = i % maxChunksPerRow;
            Image chunkImage = healthChunks[i];
            chunkImage.rectTransform.localPosition = new Vector3(column * chunkSpacing, -row * rowSpacing, 0);

            int chunkStart = i * 25;
            int remaining = Mathf.Clamp(totalHealth - chunkStart, 0, 25);

            if (remaining == 25) chunkImage.sprite = fullChunkSprite;
            else if (remaining >= 19) chunkImage.sprite = threeQuarterSprite;
            else if (remaining >= 13) chunkImage.sprite = halfSprite;
            else if (remaining >= 7) chunkImage.sprite = quarterSprite;
            else chunkImage.sprite = emptySprite;
        }
    }

    private void UpdateShield()
    {
        int totalShield = playerStats.currentShield;
        int maxShield = playerStats.GetMaxShield();
        int totalChunks = Mathf.CeilToInt(maxShield / 25f);

        for (int i = 0; i < totalChunks; i++)
        {
            int row = i / maxChunksPerRow;
            int column = i % maxChunksPerRow;
            Image chunkImage = shieldChunks[i];
            chunkImage.rectTransform.localPosition = new Vector3(column * chunkSpacing, -row * rowSpacing, 0);

            int chunkStart = i * 25;
            int remaining = Mathf.Clamp(totalShield - chunkStart, 0, 25);

            if (remaining == 25) chunkImage.sprite = shieldFullSprite;
            else if (remaining >= 19) chunkImage.sprite = shieldThreeQuarterSprite;
            else if (remaining >= 13) chunkImage.sprite = shieldHalfSprite;
            else if (remaining >= 7) chunkImage.sprite = shieldQuarterSprite;
            else chunkImage.sprite = shieldEmptySprite;
        }
    }
}
