using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
   public void StartGame()
   {
    SceneManager.LoadSceneAsync(1);
   }

   public void QuitGame()
   {
      Debug.Log("QUIT");
      Application.Quit();
   }
}
