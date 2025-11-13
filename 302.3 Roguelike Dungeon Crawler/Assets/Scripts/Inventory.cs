using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    private InventoryManager inventoryManager;


    private void Awake()
    {
        // Try to find the itemIcon
        if (itemIcon == null)
        {
            Transform childTransform = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (childTransform != null)
            {
                itemIcon = childTransform.GetComponent<Image>();
            }
            else
            {
                Debug.LogError($"InventorySlot: No child with Image component found on {gameObject.name}. Please ensure the slot has a child with an Image.");
            }
        }

        normalScale = itemIcon != null ? itemIcon.rectTransform.localScale : Vector3.one;
        hoverScale = normalScale * 1.15f;

        inventoryManager = FindFirstObjectByType<InventoryManager>();
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

    if (itemData != null && tooltip != null)
        tooltip.Show(itemData);
}

public void OnPointerExit(PointerEventData eventData)
{
    itemIcon.rectTransform.localScale = normalScale;

    // Only hide tooltip if pointer is not over the tooltip itself
    if (tooltip != null && !tooltip.IsPointerOver())
        tooltip.Hide();
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
                        // Convert ItemType to Loot.EquipmentType
                        Loot.EquipmentType lootType = EquipSlot.ConvertToLootType(itemData.itemType);
                        Debug.Log($"Pointer over {pointerObject.name}, trying to equip {itemData.itemName}");


                        if (equipSlot.AcceptItem(itemData, itemData.equipmentStats, lootType))
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
