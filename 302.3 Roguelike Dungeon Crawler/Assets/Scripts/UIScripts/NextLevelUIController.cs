using System.Collections;
using UnityEngine;

public class NextLevelUIController : MonoBehaviour
{
    [Tooltip("If > 0 the UI will auto-hide after this many seconds when the scene starts.")]
    public float autoHideDelay = 2f;

    Coroutine hideRoutine;

    // Static instance so code can reference this controller
    public static NextLevelUIController Instance { get; private set; }

    void Awake()
    {
        Instance = this;

        // If the UI is active when the scene loads, optionally auto-hide it
        if (gameObject.activeInHierarchy && autoHideDelay > 0f)
            hideRoutine = StartCoroutine(HideAfter(autoHideDelay));
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

    // Instance method: shows and schedules hide
    public void ShowTemporary(float seconds)
    {
        // ensure UI is active first
        Show();

        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }

        // pick an active MonoBehaviour to run the coroutine on (LevelManager or UIManager preferred)
        MonoBehaviour runner = null;
        var lm = FindFirstObjectByType<LevelManager>();
        if (lm != null) runner = lm;
        else
        {
            var ui = FindFirstObjectByType<UIManager>();
            if (ui != null) runner = ui;
        }

        if (runner != null)
            hideRoutine = runner.StartCoroutine(HideAfter(seconds));
        else
            hideRoutine = StartCoroutine(HideAfter(seconds));
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
        hideRoutine = null;
    }

    // Static helper: safe way to show the NextLevel UI even when its GameObject/component is inactive
    public static void ShowTemporaryGlobal(float seconds)
    {
        // If we've already got an instance, use it
        if (Instance != null)
        {
            Instance.ShowTemporary(seconds);
            return;
        }

        // Try to find any NextLevelUIController in the project/scene, including inactive ones
        var all = Resources.FindObjectsOfTypeAll<NextLevelUIController>();
        if (all != null && all.Length > 0)
        {
            // prefer the first that belongs to a scene (not assets)
            NextLevelUIController found = null;
            foreach (var c in all)
            {
                if (c == null) continue;
                // ignore prefab assets
                if (c.gameObject.scene.isLoaded)
                {
                    found = c;
                    break;
                }
                if (found == null) found = c;
            }

            if (found != null)
            {
                // make sure the GameObject is active so coroutines & Show work correctly
                if (!found.gameObject.activeInHierarchy) found.gameObject.SetActive(true);

                Instance = found;
                found.ShowTemporary(seconds);
                return;
            }
        }

        // Last resort: nothing found — log so you can track the problem
        Debug.LogWarning("NextLevelUIController.ShowTemporaryGlobal: no NextLevelUIController instance found in scene.");
    }
}
