using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Dash UI")]
    public Image dashIconOverlay;
    
    [Header("Player UI")]
    // Assign the health slider from the scene (HUD) in the Inspector.
    // The PlayerController will pick this up at runtime, which avoids needing
    // a reference to the spawned Player in the Inspector.
    public UnityEngine.UI.Slider healthSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
