using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpriteStatBar : MonoBehaviour
{
    [Tooltip("Sprites ordered from FULL (index 0) to EMPTY (last index).")]
    public Sprite[] sprites; 

    [Range(0f, 1f)]
    [Tooltip("Current normalized value 0..1 (1 = full).")]
    public float normalizedValue = 1f;

    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        UpdateSprite();
    }

    /// <summary>
    /// Call to set the normalized value (0..1).
    /// </summary>
    public void SetNormalizedValue(float value)
    {
        normalizedValue = Mathf.Clamp01(value);
        UpdateSprite();
    }

    /// <summary>
    /// Updates the image sprite based on normalizedValue.
    /// </summary>
    private void UpdateSprite()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        // Map normalizedValue (1->0) to sprite indexes (0->sprites.Length-1)
        float inverted = 1f - normalizedValue; // so full=0, empty=1
        int index = Mathf.Clamp(Mathf.RoundToInt(inverted * (sprites.Length - 1)), 0, sprites.Length - 1);
        image.sprite = sprites[index];
    }

    /// <summary>
    /// Utility: set by current and max values.
    /// </summary>
    public void SetFromCurrentMax(float current, float max)
    {
        if (max <= 0f) SetNormalizedValue(0f);
        else SetNormalizedValue(current / max);
    }
}
