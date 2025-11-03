using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private int floorNumber = 1;

    void Start()
    {
        // When the level (scene) loads, show the banner
        //FindObjectOfType<LevelBannerUI>()?.ShowBanner($"Floor {floorNumber}");
    }

}
