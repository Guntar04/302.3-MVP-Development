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

    startButton.onClick.RemoveAllListeners();
    startButton.onClick.AddListener(() => Debug.Log("Temporary test click works"));

    // Reset session state on scene load
    Debug.Log("CharacterSelect Start() called");
    canPressStart = false;
    selectedCharacterIndex = -1; // reset selection so Start is blocked
     Debug.Log($"Initial state -> selectedCharacterIndex: {selectedCharacterIndex}, canPressStart: {canPressStart}");

    // Hook up the character button listeners
      for (int i = 0; i < characterButtons.Length; i++)
    {
        int index = i; // capture for lambda
        characterButtons[i].onClick.RemoveAllListeners(); // optional, just to be safe
        characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
    }

    // Hook up Start button
      startButton.onClick.AddListener(StartGame);
}

void SelectCharacter(int index)
{
    selectedCharacterIndex = index;
    canPressStart = true; // allow Start now

    Debug.Log($"Selected character: {characterButtons[index].name}");
}

public bool CanPressStart()
{
    return canPressStart;
}


void StartGame()
{
    Debug.Log($"StartGame called -> selectedCharacterIndex: {selectedCharacterIndex}, canPressStart: {canPressStart}");
    if (!canPressStart)
    {
        Debug.Log("Select a character first!");
        return; // block Start
    }

    Debug.Log($"Starting game with character #{selectedCharacterIndex}");
    PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
    PlayerPrefs.Save();

    SceneManager.LoadScene(2);
}
}
