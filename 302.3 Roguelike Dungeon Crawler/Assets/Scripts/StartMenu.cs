using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StartMenu : MonoBehaviour
{
    public void StartGame()
    {
        // Clean up before starting new run
        LevelManager.PrepareForNewRun();
        
        // Load character selection or game scene
        SceneManager.LoadScene("CharacterSelection", LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Debug.Log("QUIT");
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false; // stops play mode in Editor
            #else
            Application.Quit();                  // quits the built game
            #endif

    }
}
