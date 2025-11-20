using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelection : MonoBehaviour
{
    private static bool canPressStart = false;

    [Header("UI Elements")]
    public Button startButton;
    public Button[] characterButtons;

    private int selectedCharacterIndex = -1;

    void Start()
    {
        // Reset session state on scene load
        //Debug.Log("CharacterSelect Start() called");
        canPressStart = false;
        selectedCharacterIndex = -1;
        //Debug.Log($"Initial state -> selectedCharacterIndex: {selectedCharacterIndex}, canPressStart: {canPressStart}");

        // Hook up the character button listeners
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.RemoveAllListeners();
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        // Hook up Start button - ONLY ONE LISTENER
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(StartGame);
    }

    void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;
        canPressStart = true;
        //Debug.Log($"Selected character: {characterButtons[index].name}");
    }

    public bool CanPressStart()
    {
        return canPressStart;
    }

    void StartGame()
    {
        //Debug.Log($"StartGame called -> selectedCharacterIndex: {selectedCharacterIndex}, canPressStart: {canPressStart}");
        
        if (!canPressStart)
        {
            Debug.Log("Select a character first!");
            return;
        }

        Debug.Log($"Starting game with character #{selectedCharacterIndex}");
        PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
        PlayerPrefs.Save();

        // Clean up BEFORE loading scene
        LevelManager.PrepareForNewRun();

        // Load game scene
        Debug.Log("Loading GameSceneDuplicate...");
        SceneManager.LoadScene("GameSceneDuplicate", LoadSceneMode.Single);
    }
}