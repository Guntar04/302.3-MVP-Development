using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameOverMenu : MonoBehaviour
{
    public void RestartGame()
   {
    SceneManager.LoadSceneAsync(1);
   }

   public void Menu()
   {
    SceneManager.LoadSceneAsync(0);
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
