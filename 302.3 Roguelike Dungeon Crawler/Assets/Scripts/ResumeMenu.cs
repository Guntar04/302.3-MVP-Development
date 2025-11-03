using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuPanel;   // The main pause menu panel
    public GameObject optionsPanel;     // Optional options sub-panel

    private bool isPaused = false;

    private void Start()
    {
        pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    private void Update()
    {
        // Press ESC to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // -----------------------------
    // Pause / Resume
    // -----------------------------
    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        Time.timeScale = 1f; // Resume game time
        isPaused = false;
    }

    public void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; // Freeze game time
        isPaused = true;
    }

    // -----------------------------
    // Button Functions
    // -----------------------------
    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Reset time scale
        SceneManager.LoadScene(0); // Scene 1 is your main menu
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Stop play mode
        #else
            Application.Quit();
        #endif
    }
}
