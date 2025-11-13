using UnityEngine;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemStatsText;

    public bool IsPointerOver()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), Input.mousePosition);
    }


 void Update()
    {
        // Follow mouse position each frame
        Vector2 mousePos = Input.mousePosition;
transform.position = mousePos + new Vector2(15f, -15f);

    }

    public void Show(ItemData item)
    {


        if (item == null) return;

        itemNameText.text = item.itemName;

        // Build stats text based on equipmentStats
        string stats = "";
        if (item.equipmentStats != null)
        {
            stats += $"Attack: {item.equipmentStats.attackPower}\n";
            stats += $"Defense: {item.equipmentStats.defense}\n";
            stats += $"Attack Speed: {item.equipmentStats.attackSpeed}\n";
        }

        itemStatsText.text = stats;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

       private string FormatStats(ItemData item)
    {
        if (item.equipmentStats == null) return "";

        var stats = item.equipmentStats;
        string result = "";

        if (stats.attackPower > 0) result += $"Attack: {stats.attackPower}\n";
        if (stats.defense > 0) result += $"Defense: {stats.defense}\n";
        if (stats.attackSpeed > 0) result += $"Attack Speed: {stats.attackSpeed}\n";

        return result.TrimEnd('\n');
    }
}
