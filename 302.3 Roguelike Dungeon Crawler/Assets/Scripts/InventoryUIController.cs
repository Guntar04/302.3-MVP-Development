using UnityEngine;
using UnityEngine.SceneManagement;

public class InventoryUIController : MonoBehaviour
{
    public GameObject InventoryCanvas;   // leave empty, will auto-find in scene
    public InventorySlot[] slots;
    public ItemData[] testItems;

    

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
        // Only assign slots if there is an InventoryCanvas in the loaded scene
        InventoryCanvas = GameObject.Find("InventoryCanvas");
        if (InventoryCanvas != null)
        {
            InventoryCanvas.SetActive(false); // hide by default

            slots = InventoryCanvas.GetComponentsInChildren<InventorySlot>(true);

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].inventoryController = this;
                slots[i].slotIndex = i;
                slots[i].ClearItem();
            }
        }
        else
        {
            Debug.LogWarning("InventoryCanvas not found in this scene.");
        }
    }

    void Update()
    {
        if (InventoryCanvas == null || slots == null) return;

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
        InventoryCanvas.SetActive(isOpen);

        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void OnInventoryButtonPressed() => ToggleInventory();

    public void AddItem(ItemData item)
    {
        if (item == null || slots == null) return;

        foreach (var slot in slots)
        {
            if (!slot.HasItem())
            {
                slot.SetItem(item);
                return;
            }
        }

        Debug.Log("Inventory full!");
    }

    public void RemoveItem(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return;
        slots[index].ClearItem();
    }
}
