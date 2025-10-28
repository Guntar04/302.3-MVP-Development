using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Slot Info")]
    public Image itemIcon;
    public ItemData itemData; // current item

    [Header("Drag Settings")]
    public GameObject dragIconPrefab; // assign in Inspector
    private GameObject currentDragIcon;

    private Vector3 normalScale;
    private Vector3 hoverScale;
    private Canvas parentCanvas;

    private InventoryManager inventoryManager;

    private void Awake()
    {
        if (itemIcon == null)
            itemIcon = transform.GetChild(0).GetComponent<Image>();

        normalScale = itemIcon.rectTransform.localScale;
        hoverScale = normalScale * 1.15f;

        inventoryManager = FindObjectOfType<InventoryManager>();
        parentCanvas = GetComponentInParent<Canvas>();

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        itemIcon.enabled = itemData != null && itemIcon.sprite != null;
    }

    public void SetItem(ItemData data)
    {
        itemData = data;
        itemIcon.sprite = data != null ? data.icon : null;
        UpdateVisibility();
    }

    public void ClearItem()
    {
        itemData = null;
        itemIcon.sprite = null;
        UpdateVisibility();
    }

    public bool HasItem() => itemData != null;

    #region Pointer Events
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
            Debug.Log($"Clicked slot — discarding {itemData.itemName}.");
            inventoryManager.RemoveItem(System.Array.IndexOf(inventoryManager.slots, this));
        }
        else
        {
            Debug.Log("Clicked empty slot — adding test item.");
            inventoryManager.AddItem(inventoryManager.testItems[0]);
        }
    }
    #endregion

    #region Drag & Drop
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem() || dragIconPrefab == null) return;

        // Create drag icon
        currentDragIcon = Instantiate(dragIconPrefab, parentCanvas.transform);
        Image iconImage = currentDragIcon.GetComponent<Image>();
        if (iconImage != null)
            iconImage.sprite = itemIcon.sprite;

        CanvasGroup cg = currentDragIcon.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentDragIcon != null)
            currentDragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentDragIcon != null)
        {
            GameObject pointerObject = eventData.pointerEnter;
            if (pointerObject != null)
            {
                // Swap with another inventory slot
                InventorySlot targetSlot = pointerObject.GetComponent<InventorySlot>();
                if (targetSlot != null)
                {
                    SwapItems(targetSlot);
                }
                else
                {
                    // Try to equip item
                    EquipSlot equipSlot = pointerObject.GetComponentInParent<EquipSlot>();

                    if (equipSlot != null)
                    {
                        if (equipSlot.AcceptItem(itemData))
                        {
                            ClearItem();
                        }
                        else
                        {
                            Debug.Log($"{itemData.itemName} cannot be equipped in {equipSlot.acceptedType} slot!");
                        }
                    }
                }
            }

            Destroy(currentDragIcon);
        }
    }

    private void SwapItems(InventorySlot targetSlot)
    {
        ItemData tempData = targetSlot.itemData;

        targetSlot.SetItem(itemData);
        SetItem(tempData);
    }
    #endregion
}
