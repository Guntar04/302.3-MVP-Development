using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LevelBannerUI : MonoBehaviour
{
    public Image bannerImage;
    public TextMeshProUGUI floorText;
    public float fadeDuration = 1f;
    public float visibleTime = 2f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // start invisible
    }

    public void ShowBanner(string floorName)
    {
        floorText.text = floorName;
        StartCoroutine(BannerRoutine());
    }

    private IEnumerator BannerRoutine()
    {
        // Fade In
        yield return Fade(0f, 1f, fadeDuration);

        // Stay visible
        yield return new WaitForSeconds(visibleTime);

        // Fade Out
        yield return Fade(1f, 0f, fadeDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
