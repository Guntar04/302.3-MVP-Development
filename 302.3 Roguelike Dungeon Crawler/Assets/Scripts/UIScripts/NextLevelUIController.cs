using System.Collections;
using UnityEngine;

public class NextLevelUIController : MonoBehaviour
{
    [Tooltip("If > 0 the UI will auto-hide after this many seconds when the scene starts.")]
    public float autoHideDelay = 2f;

    Coroutine hideRoutine;

    void Awake()
    {
        // If the UI is active when the scene loads, optionally auto-hide it
        if (gameObject.activeInHierarchy && autoHideDelay > 0f)
            hideRoutine = StartCoroutine(HideAfter(autoHideDelay));
    }

    public void Show()
    {
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }
        gameObject.SetActive(false);
    }

    public void ShowTemporary(float seconds)
    {
        Show();
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfter(seconds));
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
        hideRoutine = null;
    }
}
