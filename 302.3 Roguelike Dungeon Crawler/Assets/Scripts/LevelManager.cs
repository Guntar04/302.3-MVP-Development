using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    // Small helper used to show a banner when a specific scene loads.
    // Renamed from LevelManager to avoid collision with the runtime LevelManager in RandomMapGeneration.
    public class LevelBanner : MonoBehaviour
    {
        [SerializeField] private int floorNumber = 1;

        void Start()
        {
            // When the level (scene) loads, show the banner
            //FindObjectOfType<LevelBannerUI>()?.ShowBanner($"Floor {floorNumber}");
        }
    }
}
