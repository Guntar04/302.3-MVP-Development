using UnityEngine;
using UnityEngine.SceneManagement;

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
      Application.Quit();
   }
}
