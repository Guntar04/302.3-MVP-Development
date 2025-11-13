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

          if (LevelManager.Instance != null)
        {
            floor = LevelManager.Instance.currentFloor;
        }

        floorText.text = "Floor Reached: " + floor;
        
        // PLACEHOLDER
        enemiesText.text = "Enemies Killed: 8";
        killedByText.text = "Killed By: Goblin King";

   
        if (killedByImage != null && testEnemySprite != null)
            {
                killedByImage.sprite = testEnemySprite;
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


    public void UpdateLoseScreen(int floor, int kills, string enemyName, Sprite enemySprite)
    {
        floorText.text = "Floor Reached: " + floor;
        enemiesText.text = "Enemies Killed: " + kills;
        killedByText.text = "Killed By: " + enemyName;

        if (killedByImage != null && enemySprite != null)
        {
            killedByImage.sprite = enemySprite;
            killedByImage.color = Color.white;
        }
    }
}
