using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image itemIcon;
    private Vector3 normalScale;
    private Vector3 hoverScale;

    private InventoryManager inventoryManager; // reference to manager

    private void Awake()
    {
        if (itemIcon == null)
            itemIcon = transform.GetChild(0).GetComponent<Image>();

        normalScale = itemIcon.rectTransform.localScale;
        hoverScale = normalScale * 1.15f;

        inventoryManager = FindObjectOfType<InventoryManager>();

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        itemIcon.enabled = itemIcon.sprite != null;
    }

    public void SetItem(Sprite newSprite)
    {
        itemIcon.sprite = newSprite;
        UpdateVisibility();
    }

    public void ClearItem()
    {
        itemIcon.sprite = null;
        UpdateVisibility();
    }

    public bool HasItem()
    {
        return itemIcon.sprite != null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemIcon.enabled)
            itemIcon.rectTransform.localScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        itemIcon.rectTransform.localScale = normalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (HasItem())
        {
            Debug.Log("Clicked slot — discarding item.");
            inventoryManager.RemoveItem(System.Array.IndexOf(inventoryManager.slots, this));
        }
        else
        {
            Debug.Log("Clicked empty slot — adding item.");
            inventoryManager.AddItem(inventoryManager.testItems[0]); // test pickup
        }
    }
}
