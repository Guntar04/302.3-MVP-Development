using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ButtonScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Scale Settings")]
    public float hoverScale = 1.1f;
    public float selectedScale = 1.2f;
    public bool isSelectable = false;

    [Header("Start Button Reference")]
    public Button startButton;

    private Vector3 originalScale;
    private bool isSelected = false;
    //private static bool canPressStart = false; // shared flag across cards

    void Start()
    {
        originalScale = transform.localScale;
        // Make sure start button looks normal and stays visually active
        if (startButton != null)
            startButton.interactable = true; 
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected)
            transform.localScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
            transform.localScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable) return;

        isSelected = true;
        transform.localScale = originalScale * selectedScale;

        // Allow Start button functionality after any card is clicked
        //canPressStart = true;
    }

    // Hook this to the Start button OnClick in Inspector
    public void OnStartButtonClicked()
{
    // Only proceed if a character has been selected
    CharacterSelection selection = FindFirstObjectByType<CharacterSelection>();
    if (selection != null && selection.CanPressStart()) // add a public getter
    {
        Debug.Log("Starting the game from ButtonScaler");
        SceneManager.LoadScene(2);
    }
    else
    {
        Debug.Log("Cannot start: no character selected");
    }
}
}