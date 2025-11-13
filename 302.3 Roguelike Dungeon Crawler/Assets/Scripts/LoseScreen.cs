using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoseScreenUI : MonoBehaviour
{
    [Header("Stat Texts")]
    public TextMeshProUGUI floorText;
    public TextMeshProUGUI enemiesText;
    public TextMeshProUGUI killedByText;

    [Header("Killed By Image")]
    public Image killedByImage; 

    [Header("Loot Icons")]
    public Image[] lootIcons;
    public Sprite[] testLootSprites; // PLACEHOLDER

    [Header("Tip Text")]
    public TextMeshProUGUI tipText;

     [Header("Test Enemy Sprite")] 
    public Sprite testEnemySprite; // PLACEHOLDER

    void Start()
    {
          int floor = 1;
          int kills = 0;

          if (LevelManager.Instance != null)
        {
            floor = LevelManager.Instance.currentFloor;
              kills = LevelManager.Instance.enemiesKilled;
        }

        floorText.text = "Floor Reached: " + floor;
        enemiesText.text = "Enemies Killed: " + kills;
   
        if (killedByText != null)
        killedByText.text = "Killed By: " + (GameData.EnemyName ?? "Unknown");
        

    if (killedByImage != null && GameData.EnemySprite != null)
    {
        killedByImage.sprite = GameData.EnemySprite;
        killedByImage.color = Color.white;
    }

        for (int i = 0; i < lootIcons.Length; i++)
        {
            if (i < testLootSprites.Length && testLootSprites[i] != null)
            {
                lootIcons[i].sprite = testLootSprites[i];
            }
            else
            {
                lootIcons[i].enabled = false; // hide empty ones
            }
        }

        tipText.text = "Tip: Blocking reduces more damage than dodging";
    }
}
