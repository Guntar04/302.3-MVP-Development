using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class InventoryUIController : MonoBehaviour
{
    public GameObject InventoryCanvas;   // will auto-find in scene
    public InventorySlot[] slots;
    public ItemData[] testItems;

    // ---------------------------
    // Persistent inventory data
    private List<ItemData> inventoryItems = new List<ItemData>();
    private bool isOpen = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InventoryCanvas = GameObject.Find("InventoryCanvas");
        if (InventoryCanvas != null)
        {
            slots = InventoryCanvas.GetComponentsInChildren<InventorySlot>(true);
            

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].inventoryController = this;
                slots[i].slotIndex = i;

                if (i < inventoryItems.Count)
                    slots[i].SetItem(inventoryItems[i]);
                else
                    slots[i].ClearItem();
            }

            RefreshUI();
CanvasGroup cg = InventoryCanvas.GetComponent<CanvasGroup>();
if (cg != null)
{
    cg.alpha = 0f;            // hidden
    cg.interactable = false;  // cannot click
    cg.blocksRaycasts = false;// ignore mouse
}

isOpen = false;    

             Debug.Log($"InventoryCanvas found. Slots count: {slots.Length}");

             if (slots == null || slots.Length == 0)
{
    Debug.LogWarning("Slots not ready!");
    Debug.Break(); // pauses the editor so you can inspect variables
}

        }
        else
        {
            Debug.LogWarning("InventoryCanvas not found in this scene.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            ToggleInventory();

        if (Input.GetKeyDown(KeyCode.E) && testItems != null && testItems.Length > 0)
            AddItem(testItems[Random.Range(0, testItems.Length)]);
    }

    public void ToggleInventory()
{
    if (InventoryCanvas == null) return;

    isOpen = !isOpen;

    CanvasGroup cg = InventoryCanvas.GetComponent<CanvasGroup>();
    if (cg != null)
    {
        cg.alpha = isOpen ? 1f : 0f;               // visible or invisible
        cg.interactable = isOpen;                  // can interact when open
        cg.blocksRaycasts = isOpen;                // can click when open
    }

    Cursor.visible = isOpen;
    Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
}

    public void OnInventoryButtonPressed() => ToggleInventory();

    public bool AddItem(ItemData item)
{
    if (item == null) return false;

    // Add to persistent inventory first
    inventoryItems.Add(item);

    // Force UI to refresh even if canvas is hidden
     if (slots != null)
    {
        RefreshUI();  // update the slots
    }
    

    Debug.Log($"Item {item.itemName} added to inventory (UI updated).");
    return true;
}

private void RefreshUI()
{
    if (slots == null) return;

    for (int i = 0; i < slots.Length; i++)
    {
        if (i < inventoryItems.Count)
            slots[i].SetItem(inventoryItems[i]);
        else
            slots[i].ClearItem();
    }
}




    public void RemoveItem(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return;

        // Remove from persistent data
        inventoryItems.RemoveAt(index);

        // Update UI
        if (slots != null && index < slots.Length)
            slots[index].ClearItem();

        // Refresh UI slots after removed item
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventoryItems.Count)
                slots[i].SetItem(inventoryItems[i]);
            else
                slots[i].ClearItem();
        }
    }
}
