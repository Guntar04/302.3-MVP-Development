using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statsText;

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

        rectTransform.pivot = new Vector2(0, 1); 
        rectTransform.position = Input.mousePosition + new Vector3(offset.x, offset.y, 0);
    }

    public void Show(ItemData item)
    {
        if (item == null) return;

        rectTransform.pivot = new Vector2(0f, 1f);

        // NAME
        nameText.text = $"<b>{item.itemName}</b>";

        statsText.text = "";

        // --- FIXED TOOLTIP LOGIC ---
        if (item.equipmentStats != null)
        {
            // Weapon → ATK only
            if (item.itemType == ItemType.Weapon)
            {
                statsText.text += $"<color=#FF4A4A>Attack: +{item.equipmentStats.attackPower}</color>\n";
            }

            // Armor → DEF only
            if (item.itemType == ItemType.Chestplate ||
                item.itemType == ItemType.Helmet ||
                item.itemType == ItemType.Pants ||
                item.itemType == ItemType.Boots ||
                item.itemType == ItemType.Shield)
            {
                statsText.text += $"<color=#4AFF6C>Defense: +{item.equipmentStats.defense}</color>\n";
            }
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
