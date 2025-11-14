using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, 
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Slot Info")]
    public Image itemIcon;
    public ItemData itemData; // current item
    public ItemTooltip tooltip;

    [Header("Drag Settings")]
    public GameObject dragIconPrefab; // assign in Inspector
    private GameObject currentDragIcon;

    private Vector3 normalScale;
    private Vector3 hoverScale;
    private Canvas parentCanvas;

    [HideInInspector] public InventoryUIController inventoryController; // hook to UIController
    [HideInInspector] public int slotIndex; // optional: store index for clicks

    private void Start()
    {
        // ---------- Find itemIcon safely ----------
        if (itemIcon == null)
        {
            Transform childTransform = transform.Find("Icon"); // exact child name
            if (childTransform != null)
                itemIcon = childTransform.GetComponent<Image>();
            else
                Debug.LogError($"InventorySlot: No child with Image component found on {gameObject.name}. Make sure each slot has a child named 'Icon'.");
        }

        if (itemIcon == null) return; // Prevent further null errors

        normalScale = itemIcon.rectTransform.localScale;
        hoverScale = normalScale * 1.15f;

        // ---------- Find parent canvas ----------
        parentCanvas = GetComponentInParent<Canvas>();

        // ---------- Initialize slot ----------
        ClearItem();
    }

    private void UpdateVisibility()
    {
        if (itemIcon == null) return; // safety check
        itemIcon.enabled = itemData != null && itemIcon.sprite != null;
    }

    public void SetItem(ItemData data)
    {
        itemData = data;
        if (itemIcon != null)
            itemIcon.sprite = data != null ? data.icon : null;

        UpdateVisibility();
    }

    public void ClearItem()
    {
        itemData = null;
        if (itemIcon != null)
            itemIcon.sprite = null;

        UpdateVisibility();
    }

    public bool HasItem() => itemData != null;

    #region Pointer Events
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemIcon != null)
            itemIcon.rectTransform.localScale = hoverScale;

        if (itemData != null && tooltip != null)
            tooltip.Show(itemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (itemIcon != null)
            itemIcon.rectTransform.localScale = normalScale;

        if (tooltip != null)
            tooltip.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryController == null) return;

        if (HasItem())
        {
            Debug.Log($"Clicked slot — discarding {itemData.itemName}.");
            inventoryController.RemoveItem(slotIndex);
        }
        else if (inventoryController.testItems != null && inventoryController.testItems.Length > 0)
        {
            Debug.Log("Clicked empty slot — adding test item.");
            inventoryController.AddItem(inventoryController.testItems[0]);
        }
    }
    #endregion

    #region Drag & Drop
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem() || dragIconPrefab == null || parentCanvas == null) return;

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
                InventorySlot targetSlot = pointerObject.GetComponent<InventorySlot>();
                if (targetSlot != null)
                {
                    SwapItems(targetSlot);
                }
                else
                {
                    EquipSlot equipSlot = pointerObject.GetComponentInParent<EquipSlot>();
                    if (equipSlot != null && itemData != null)
                    {
                        Loot.EquipmentType lootType = EquipSlot.ConvertToLootType(itemData.itemType);

                        if (equipSlot.AcceptItem(itemData, itemData.equipmentStats, lootType))
                            ClearItem();
                        else
                            Debug.Log($"{itemData.itemName} cannot be equipped in {equipSlot.acceptedType} slot!");
                    }
                }
            }

            Destroy(currentDragIcon);
        }
    }

    private void SwapItems(InventorySlot targetSlot)
    {
        if (targetSlot == null) return;
        ItemData tempData = targetSlot.itemData;
        targetSlot.SetItem(itemData);
        SetItem(tempData);
    }
    #endregion
}
