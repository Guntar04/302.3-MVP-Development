using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statsText;

    // Offset from mouse cursor
    public Vector2 offset = new Vector2(15f, -15f);

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

    // Move tooltip so top-left corner touches the mouse
    rectTransform.pivot = new Vector2(0, 1); // top-left
    rectTransform.position = Input.mousePosition + new Vector3(offset.x, offset.y, 0);
    }

    public void Show(ItemData item)
    {
        if (item == null) return;

        rectTransform.pivot = new Vector2(0f, 1f); // top-left corner is pivot

        // NAME
        nameText.text = $"<b>{item.itemName}</b>";

        // STATS (colored)
        statsText.text = "";
        if (item.equipmentStats != null)
        {
            if (item.equipmentStats.attackPower != 0)
                statsText.text += $"<color=#FF4A4A>ATK: {item.equipmentStats.attackPower}</color>\n";

            if (item.equipmentStats.attackSpeed != 0)
                statsText.text += $"<color=#4AA3FF>SPEED: {item.equipmentStats.attackSpeed}</color>\n";

            if (item.equipmentStats.defense != 0)
                statsText.text += $"<color=#4AFF6C>HP: {item.equipmentStats.defense}</color>\n";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform.parent as RectTransform);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
