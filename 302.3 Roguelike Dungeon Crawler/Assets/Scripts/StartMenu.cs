using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StartMenu : MonoBehaviour
{
   public void StartGame()
   {
      // Clear saved run progress so a new game starts fresh
      PlayerProgress.ResetProgress();

      // Load gameplay scene (index 1)
      SceneManager.LoadSceneAsync(1);
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
