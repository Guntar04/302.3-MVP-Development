using UnityEngine;
using UnityEngine.SceneManagement;

// Standalone component to destroy the attached GameObject when a new scene is loaded.
// Keeping it as a separate public class avoids issues with nested/non-public component types.
public class LootCleanup : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Destroy(gameObject);
    }
}
