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
    public  List<ItemData> inventoryItems = new List<ItemData>();
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

isOpen = false;    

             Debug.Log($"InventoryCanvas found. Slots count: {slots.Length}");

             if (slots == null || slots.Length == 0)
{
    Debug.LogWarning("Slots not ready!");
    Debug.Break(); // pauses the editor so you can inspect variables
}

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


}

    public void OnInventoryButtonPressed() => ToggleInventory();

public bool AddItem(ItemData item, bool allowDuplicate = true)
{
    if (item == null) return false;

    // Check duplicates only if not allowed
    if (!allowDuplicate && inventoryItems.Contains(item))
    {
        Debug.LogWarning($"Item {item.itemName} already in inventory! Skipping AddItem.");
        return false;
    }

    inventoryItems.Add(item);
    RefreshUI();

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




// Removes an item using its index in the inventory list (original behaviour)
public void RemoveItem(int index)
{
    if (index < 0 || index >= inventoryItems.Count) return;

    // Remove from the list
    inventoryItems.RemoveAt(index);

    // Rebuild UI
    if (slots != null)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventoryItems.Count)
                slots[i].SetItem(inventoryItems[i]);
            else
                slots[i].ClearItem();
        }
    }
}

// Remove item by reference
public void RemoveItem(ItemData item)
{
    if (item == null) return;

    int index = inventoryItems.IndexOf(item);
    if (index >= 0)
        RemoveItem(index); // calls the existing method
}



public string[] GetInventoryNames()
{
    string[] names = new string[inventoryItems.Count];
    for (int i = 0; i < inventoryItems.Count; i++)
    {
        names[i] = inventoryItems[i]?.itemName ?? "null";
    }
    return names;
}


}
