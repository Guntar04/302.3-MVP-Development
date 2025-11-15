using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu; // assign your PauseMenu panel
    public Button resumeButton;
    public Button menuButton;
    public Button exitButton;

    private bool isPaused = false;

    void Start()
    {
        // Hide menu on start
        pauseMenu.SetActive(false);

        // Hook up buttons
        resumeButton.onClick.AddListener(TogglePause);
        menuButton.onClick.AddListener(GoToMenu);
        exitButton.onClick.AddListener(ExitGame);
    }

    void Update()
    {
        // Open/close pause menu on ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        pauseMenu.SetActive(isPaused);

        if (isPaused)
            Time.timeScale = 0f; // pause game
        else
            Time.timeScale = 1f; // resume game
    }

    void GoToMenu()
    {
        Time.timeScale = 1f; // unpause
        SceneManager.LoadScene("StartMenu"); // replace with your start menu scene name
    }

    void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // stop play mode in editor
#endif
    }
}
