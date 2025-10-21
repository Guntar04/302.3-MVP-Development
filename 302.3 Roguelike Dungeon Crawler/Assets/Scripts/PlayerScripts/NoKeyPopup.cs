using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class NoKeyPopup : MonoBehaviour
{
    public float lifetime = 2f;
    public Vector2 screenOffset = new Vector2(0f, 60f);
    [Tooltip("0..1 smoothing factor; 0 = snap, higher = smoother follow")]
    [Range(0f, 1f)]
    public float followSmooth = 0.2f;

    RectTransform rt;
    RectTransform canvasRT;
    Canvas parentCanvas;
    Camera uiCamera;
    Transform target;
    float timer;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        // ensure centered pivot so anchoredPosition is intuitive
        rt.pivot = new Vector2(0.5f, 0.5f);
        // ensure anchors are centered to make positioning consistent
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
    }

    // Initialize with the player transform and the canvas the popup should live under
    public void Initialize(Transform player, Canvas canvas, float duration)
    {
        target = player;
        parentCanvas = canvas;
        canvasRT = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;
        uiCamera = parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ? parentCanvas.worldCamera : null;
        lifetime = duration > 0f ? duration : lifetime;
        timer = lifetime;

        // ensure popup is visible on top
        transform.SetAsLastSibling();

        // immediately set correct anchored position so it doesn't flash bottom-left
        UpdatePosition(true);
    }

    // Refresh timer (useful if player repeatedly tries to open the exit)
    public void Refresh(float duration)
    {
        timer = duration > 0f ? duration : lifetime;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // still try to keep following canvas even if trigger exited
        if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
        if (canvasRT == null && parentCanvas != null) canvasRT = parentCanvas.GetComponent<RectTransform>();

        UpdatePosition(false);

        timer -= Time.deltaTime;
        if (timer <= 0f) Destroy(gameObject);
    }

    private void UpdatePosition(bool snap)
    {
        if (target == null || parentCanvas == null || canvasRT == null) return;

        // choose the correct camera for world->screen conversion
        Camera cam = uiCamera != null ? uiCamera : (Camera.main != null ? Camera.main : null);
        Vector2 screenPoint = cam != null ? (Vector2)cam.WorldToScreenPoint(target.position) : Vector2.zero;

        Vector2 anchored;
        // for ScreenSpace-Overlay, uiCamera should be null; for ScreenSpace-Camera pass the canvas camera
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPoint + screenOffset, uiCamera, out anchored);

        if (snap || followSmooth <= 0f)
            rt.anchoredPosition = anchored;
        else
            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, anchored, Mathf.Clamp01(followSmooth));
    }
}
