using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUIController : MonoBehaviour
{
    public GameObject inventoryCanvas;
    private bool isOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryCanvas.SetActive(isOpen);

        if (isOpen)
        {
            // Show cursor for UI interaction
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // Hide cursor for gameplay
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void OnInventoryButtonPressed()
    {
        ToggleInventory();
    }
}
