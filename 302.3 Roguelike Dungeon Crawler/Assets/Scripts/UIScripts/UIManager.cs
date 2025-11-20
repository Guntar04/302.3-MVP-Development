using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Dash UI")]
    public Image dashIconOverlay;
    
    [Header("Player UI")]
    public Slider healthSlider;
    public GameObject shieldContainer;
    public ShieldUI shieldUI;

    [Header("Scene Persistence")]
    public string[] keepOnScenes = new string[] { "SampleScene", "GameSceneDuplicate" };

    private PlayerController boundPlayerController;
    private Image healthBarFillImage;
    private float targetFillAmount = 1f;
    private bool hasInitializedHealthBar = false;

    void Awake()
    {
        Debug.Log($"=== UIManager.Awake === Scene: {SceneManager.GetActiveScene().name}");
        
        if (Instance != null && Instance != this)
        {
            Debug.Log($"UIManager: Destroying duplicate instance in scene {SceneManager.GetActiveScene().name}");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Debug.Log("UIManager: Set as singleton instance");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (boundPlayerController != null)
        {
            boundPlayerController.OnHealthChanged -= UpdateHealthSlider;
            boundPlayerController = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"=== UIManager.OnSceneLoaded === Scene: {scene.name}, Mode: {mode}");
        
        // Reset health bar references when loading a new scene
        healthBarFillImage = null;
        hasInitializedHealthBar = false;
        
        if (keepOnScenes != null && keepOnScenes.Length > 0)
        {
            bool keep = keepOnScenes.Any(s => string.Equals(s, scene.name, System.StringComparison.OrdinalIgnoreCase));
            
            if (!keep)
            {
                Debug.Log($"UIManager: scene '{scene.name}' not in keepOnScenes -> destroying HUD");
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"UIManager: scene '{scene.name}' IS in keepOnScenes -> staying alive");
            }
        }
    }

    void LateUpdate()
    {
        if (healthBarFillImage != null)
        {
            if (Mathf.Abs(healthBarFillImage.fillAmount - targetFillAmount) > 0.01f)
            {
                Debug.LogWarning($"UIManager LateUpdate: fillAmount was {healthBarFillImage.fillAmount:F2} but should be {targetFillAmount:F2} - FORCING IT");
            }
            
            healthBarFillImage.fillAmount = targetFillAmount;
        }
    }

    public void BindPlayer(GameObject player)
    {
        if (player == null) return;
        Debug.Log($"UIManager.BindPlayer called for player: {player.name} in scene: {SceneManager.GetActiveScene().name}");
        StartCoroutine(BindPlayerRoutine(player));
    }

    private IEnumerator BindPlayerRoutine(GameObject player)
    {
        yield return null;
        yield return null;

        if (player == null) yield break;

        Debug.Log($"=== UIManager.BindPlayerRoutine START === Player: {player.name}");
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"hasInitializedHealthBar: {hasInitializedHealthBar}");

        // Find the HealthBar Image - SEARCH IN CURRENT SCENE ONLY
        if (healthBarFillImage == null || !hasInitializedHealthBar)
        {
            Debug.Log("Searching for HealthBar...");
            
            Transform healthBarTransform = null;
            
            // METHOD 1: Search in THIS UIManager's hierarchy
            var hudCanvas = transform.Find("HUDCanvas");
            Debug.Log($"Method 1 - Looking for HUDCanvas as child of UIManager: {(hudCanvas != null ? "FOUND" : "NOT FOUND")}");
            
            if (hudCanvas != null)
            {
                var healthUI = hudCanvas.Find("HealthUI");
                Debug.Log($"  - Looking for HealthUI under HUDCanvas: {(healthUI != null ? "FOUND" : "NOT FOUND")}");
                
                if (healthUI != null)
                {
                    // Disable the Slider first
                    if (healthSlider == null)
                    {
                        healthSlider = healthUI.GetComponent<Slider>();
                    }
                    if (healthSlider != null)
                    {
                        healthSlider.fillRect = null;
                        healthSlider.enabled = false;
                        Debug.Log("    - DISABLED Slider and cleared its fillRect reference");
                    }
                    
                    healthBarTransform = healthUI.Find("HealthBar");
                    Debug.Log($"  - Looking for HealthBar under HealthUI: {(healthBarTransform != null ? "FOUND" : "NOT FOUND")}");
                }
            }
            
            // METHOD 2: Search ALL objects in scene if Method 1 failed
            if (healthBarTransform == null)
            {
                Debug.Log("Method 2 - Searching ALL canvases in scene...");
                
                var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"  - Found {allCanvases.Length} total canvases in scene");
                
                foreach (var canvas in allCanvases)
                {
                    Debug.Log($"  - Checking canvas: {canvas.name} (parent: {(canvas.transform.parent != null ? canvas.transform.parent.name : "ROOT")})");
                    
                    var healthUI = canvas.transform.Find("HealthUI");
                    if (healthUI != null)
                    {
                        Debug.Log($"    - Found HealthUI under this canvas!");
                        
                        healthBarTransform = healthUI.Find("HealthBar");
                        if (healthBarTransform != null)
                        {
                            Debug.Log($"    - Found HealthBar! Full path: {GetFullPath(healthBarTransform)}");
                            
                            // Also disable this slider
                            var slider = healthUI.GetComponent<Slider>();
                            if (slider != null)
                            {
                                slider.fillRect = null;
                                slider.enabled = false;
                                Debug.Log("    - DISABLED this Slider too");
                            }
                            break;
                        }
                    }
                }
            }
            
            if (healthBarTransform != null)
            {
                healthBarFillImage = healthBarTransform.GetComponent<Image>();
                
                if (healthBarFillImage != null)
                {
                    healthBarFillImage.type = Image.Type.Filled;
                    healthBarFillImage.fillMethod = Image.FillMethod.Horizontal;
                    healthBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                    
                    hasInitializedHealthBar = true;
                    
                    Debug.Log($"✓ SUCCESS: Cached HealthBar image");
                    Debug.Log($"  - Path: {GetFullPath(healthBarFillImage.transform)}");
                    Debug.Log($"  - InstanceID: {healthBarFillImage.GetInstanceID()}");
                    Debug.Log($"  - Initial fillAmount: {healthBarFillImage.fillAmount}");
                }
                else
                {
                    Debug.LogError("✗ FAILED: HealthBar has no Image component!");
                }
            }
            else
            {
                Debug.LogError("✗ FAILED: Could not find HealthBar transform!");
            }
        }
        else
        {
            Debug.Log($"Using existing healthBarFillImage (InstanceID: {healthBarFillImage.GetInstanceID()})");
        }

        // Unbind previous player
        if (boundPlayerController != null)
        {
            boundPlayerController.OnHealthChanged -= UpdateHealthSlider;
            boundPlayerController = null;
            Debug.Log("Unbound previous player");
        }

        // Bind to new player
        var pc = player.GetComponent<PlayerController>();
        if (pc != null && healthBarFillImage != null)
        {
            boundPlayerController = pc;
            pc.OnHealthChanged += UpdateHealthSlider;
            
            Debug.Log($"✓ Bound to player - health={pc.health}/{pc.maxHealth}");
            
            UpdateHealthSlider(pc.health, pc.maxHealth);
        }
        else
        {
            Debug.LogError($"✗ Binding FAILED - PC={pc != null}, HealthBarImage={healthBarFillImage != null}");
        }

        // Bind shield
        var playerShield = player.GetComponentInChildren<Shield>(true);
        if (playerShield != null && shieldUI != null)
        {
            shieldUI.BindShield(playerShield);
            Debug.Log("✓ Bound shield UI");
        }
        
        Debug.Log("=== UIManager.BindPlayerRoutine END ===");
    }

    private void UpdateHealthSlider(int currentHealth, int maxHealth)
    {
        if (healthBarFillImage == null)
        {
            Debug.LogError("UIManager.UpdateHealthSlider: healthBarFillImage is NULL!");
            return;
        }

        targetFillAmount = (maxHealth > 0) ? ((float)currentHealth / (float)maxHealth) : 0f;
        healthBarFillImage.fillAmount = targetFillAmount;
        
        Debug.Log($"UIManager.UpdateHealthSlider: Set to {targetFillAmount:F2} ({currentHealth}/{maxHealth}) on {GetFullPath(healthBarFillImage.transform)} [ID:{healthBarFillImage.GetInstanceID()}]");
    }

    private string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
