using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TMPHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI tmpText;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.cyan;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        tmpText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tmpText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tmpText.color = normalColor;
    }
}

