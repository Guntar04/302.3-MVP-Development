using UnityEngine;
using UnityEngine.SceneManagement;

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
      Application.Quit();
   }
}
