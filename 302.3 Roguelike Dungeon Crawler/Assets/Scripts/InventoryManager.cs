using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public InventorySlot[] slots;     // Assign your slots in the Inspector
    public Sprite[] testItems;        // Add a few test item sprites here

    private void Start()
    {
        // Optional: clear all slots at start
        foreach (var slot in slots)
            slot.ClearItem();
    }

    private void Update()
    {
        // Press E to "pick up" a random test item
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (testItems.Length > 0)
            {
                Sprite randomItem = testItems[Random.Range(0, testItems.Length)];
                AddItem(randomItem);
                Debug.Log("Picked up an item!");
            }
        }

        // Press R to "drop" all items (clear inventory)
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var slot in slots)
                slot.ClearItem();

            Debug.Log("Inventory cleared!");
        }
    }

    // Add an item to the first empty slot
    public bool AddItem(Sprite itemSprite)
    {
        foreach (var slot in slots)
        {
            if (!slot.HasItem())
            {
                slot.SetItem(itemSprite);
                return true;
            }
        }

        Debug.Log("Inventory full!");
        return false;
    }

    // Remove an item by index
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < slots.Length)
            slots[index].ClearItem();
    }
}
