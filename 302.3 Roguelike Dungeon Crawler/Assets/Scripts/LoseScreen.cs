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
    killedByText.text = "Killed By: " + (PlayerController.PlayerDeathInfo.EnemyName ?? "Unknown");

if (killedByImage != null && PlayerController.PlayerDeathInfo.EnemySprite != null)
{
    killedByImage.sprite = PlayerController.PlayerDeathInfo.EnemySprite;
    killedByImage.color = Color.white;
}


        for (int i = 0; i < lootIcons.Length; i++)
    {
        if (i < GameData.CollectedLoot.Count && GameData.CollectedLoot[i] != null)
        {
            lootIcons[i].sprite = GameData.CollectedLoot[i];
            lootIcons[i].enabled = true;
        }
        else
        {
            lootIcons[i].enabled = false; // hide empty slots
        }
    }

    tipText.text = "Tip: Blocking reduces more damage than dodging";
}
}
