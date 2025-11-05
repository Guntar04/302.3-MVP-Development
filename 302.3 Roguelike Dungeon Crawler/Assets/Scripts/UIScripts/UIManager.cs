using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Dash UI")]
    public Image dashIconOverlay;
    
    [Header("Player UI")]
    public Slider healthSlider;
    public GameObject shieldContainer;

    private void Awake()
    {
        Instance = this;
    }

    // Call after a new player is spawned so UI rebinds to the new player instance
    public void BindPlayer(GameObject player)
    {
        if (player == null) return;

        // update health UI
        var pc = player.GetComponent<PlayerController>();
        if (pc != null && healthSlider != null)
        {
            healthSlider.maxValue = Mathf.Clamp(pc.maxHealth, 1, 20);
            healthSlider.value = Mathf.Clamp(pc.health, 0, pc.maxHealth);
        }

        // Try to find the ShieldUI in the HUD (prefer explicit container)
        Component shieldUIScript = null;
        if (shieldContainer != null)
            shieldUIScript = shieldContainer.GetComponentInChildren(typeof(Component), true)
                           .GetComponentsInChildren<Component>(true)
                           .FirstOrDefault(c => c.GetType().Name.IndexOf("ShieldUI", System.StringComparison.OrdinalIgnoreCase) >= 0);

        if (shieldUIScript == null)
        {
            // fallback: search scene for a component with "ShieldUI" in its type name
            // Use FindObjectsByType to avoid deprecated API and avoid unnecessary sorting for better performance.
            var allComps = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            shieldUIScript = allComps.FirstOrDefault(c => c != null && c.GetType().Name.IndexOf("ShieldUI", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Find player's shield component (try common names first)
        Component playerShieldComp = player.GetComponent("Shield") as Component
                                      ?? player.GetComponent("ShieldController") as Component
                                      ?? player.GetComponent("Armor") as Component;

        if (playerShieldComp == null)
        {
            // try to pick any component whose type name contains "shield" or "armor"
            playerShieldComp = player.GetComponents<Component>()
                                     .FirstOrDefault(c => c != null && (c.GetType().Name.IndexOf("shield", System.StringComparison.OrdinalIgnoreCase) >= 0
                                                                      || c.GetType().Name.IndexOf("armor", System.StringComparison.OrdinalIgnoreCase) >= 0));
        }

        if (shieldUIScript != null && playerShieldComp != null)
        {
            // set the PlayerShield field/property on ShieldUI via reflection (covers private/public variations)
            var uiType = shieldUIScript.GetType();
            var fld = uiType.GetField("PlayerShield", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fld != null && fld.FieldType.IsAssignableFrom(playerShieldComp.GetType()))
            {
                fld.SetValue(shieldUIScript, playerShieldComp);
            }
            else
            {
                var prop = uiType.GetProperty("PlayerShield", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType.IsAssignableFrom(playerShieldComp.GetType()) && prop.CanWrite)
                    prop.SetValue(shieldUIScript, playerShieldComp);
            }

            // try to call common refresh/update methods on the ShieldUI so visuals update immediately
            string[] tryMethods = { "Refresh", "RefreshUI", "UpdateUI", "UpdateShieldDisplay", "OnShieldChanged", "RebuildUI" };
            foreach (var name in tryMethods)
            {
                var m = uiType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null)
                {
                    try { m.Invoke(shieldUIScript, null); } catch { }
                    break;
                }
            }

            // last resort: SendMessage to the ShieldUI component
            var mb = shieldUIScript as MonoBehaviour;
            if (mb != null)
            {
                mb.SendMessage("OnShieldChanged", SendMessageOptions.DontRequireReceiver);
                mb.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
            }

            return; // done — ShieldUI now bound and updated
        }

        // Fallback: update simple shield icons under shieldContainer (enable first N children)
        int shieldCount = 0;
        if (playerShieldComp != null)
        {
            // try to find an int field/property on the player's shield component
            var t = playerShieldComp.GetType();
            var f = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(x => x.FieldType == typeof(int));
            if (f != null) shieldCount = (int)f.GetValue(playerShieldComp);
            else
            {
                var p = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(x => x.PropertyType == typeof(int));
                if (p != null) shieldCount = (int)p.GetValue(playerShieldComp);
            }
        }

        if (shieldContainer != null)
        {
            var children = shieldContainer.transform.Cast<Transform>().ToArray();
            for (int i = 0; i < children.Length; i++)
                children[i].gameObject.SetActive(i < shieldCount);
        }
    }
}
