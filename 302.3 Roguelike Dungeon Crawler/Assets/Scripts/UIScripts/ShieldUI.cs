using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ShieldUI : MonoBehaviour
{
    [Header("Shield UI Settings")]
    public Image[] shieldIcons; // Array of shield icons
    public Sprite filledSprite; // Sprite for filled shields
    public Sprite emptySprite;  // Sprite for empty shields
    public Shield playerShield;
    private Component boundShieldComponent;
    private Coroutine pollCoroutine;
    private Coroutine reconcileCoroutine;

    [Header("Detection / Debug")]
    [Tooltip("If set, will try this exact field/property name first (e.g. 'CurrentShields' or 'shieldCount').")]
    public string overrideMemberName;
    public bool verboseLogging = false;

    // cached detected member
    private FieldInfo cachedField;
    private PropertyInfo cachedProperty;

    private void OnEnable()
    {
        // make sure icons are populated before we try to refresh visuals
        TryAutoPopulateIcons();
        // make sure visuals reflect current binding
        RefreshFromBound();
    }

    private void Start()
    {
        // If no icons assigned in inspector (possible after scene merge), try to auto-populate from children
        TryAutoPopulateIcons();
    }

    // Called by UIManager to bind a typed Shield component
    public void BindShield(Shield shield)
    {
        if (shield == null) { Unbind(); return; }

        Unbind();

        // ensure icons are available before any immediate update
        TryAutoPopulateIcons();

        playerShield = shield;
        boundShieldComponent = null;
        ClearCachedMember();

        // subscribe to runtime shield changes so UI updates when shields are consumed/added
        playerShield.OnShieldChanged += UpdateShieldUI;

        // immediate update to reflect current state
        UpdateShieldUI(playerShield.CurrentShields);

        Debug.Log($"ShieldUI.BindShield -> typed Shield instanceID {playerShield.GetInstanceID()} maxShields={playerShield.maxShields} CurrentShields={playerShield.CurrentShields}");
    }

    // Generic bind that accepts any Component (used via reflection)
    public void BindShield(Component shieldComp)
    {
        if (shieldComp == null) { Unbind(); return; }

        Unbind();

        // ensure icons are available before any immediate update
        TryAutoPopulateIcons();

        // if it's a typed Shield, use typed path
        var typed = shieldComp as Shield;
        if (typed != null)
        {
            BindShield(typed);
            return;
        }

        // otherwise remember component and try to hook event via reflection
        boundShieldComponent = shieldComp;
        playerShield = null;
        ClearCachedMember();

        Debug.Log($"ShieldUI.BindShield -> reflected component {shieldComp.GetType().Name} instanceID {shieldComp.GetInstanceID()}");

        // try to subscribe to an event named OnShieldChanged that provides int parameter
        var evt = shieldComp.GetType().GetEvent("OnShieldChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (evt != null)
        {
            try
            {
                var handler = Delegate.CreateDelegate(evt.EventHandlerType, this, nameof(OnReflectedShieldEvent));
                evt.AddEventHandler(shieldComp, handler);
                if (verboseLogging) Debug.Log($"ShieldUI: subscribed to reflected OnShieldChanged on {shieldComp.GetType().Name}");
            }
            catch
            {
                if (verboseLogging) Debug.Log($"ShieldUI: failed to subscribe to reflected event on {shieldComp.GetType().Name}, will poll");
                StartPolling();
            }
        }
        else
        {
            if (verboseLogging) Debug.Log($"ShieldUI: no event found on {shieldComp.GetType().Name}, will poll");
            StartPolling();
        }

        // cache member info for fast reads
        CacheMemberForComponent(boundShieldComponent);

        // immediate refresh from reflected value
        int val = ReadShieldIntFromComponent(boundShieldComponent);
        Debug.Log($"ShieldUI: initial reflected read -> {val}");
        UpdateShieldUI(val);

        // small reconcile in case of race between binding and component initialization
        StartReconcileAfterBind();
    }

    private void StartReconcileAfterBind()
    {
        if (reconcileCoroutine != null) StopCoroutine(reconcileCoroutine);
        reconcileCoroutine = StartCoroutine(ReconcileAfterBindRoutine());
    }

    private IEnumerator ReconcileAfterBindRoutine()
    {
        // try a few times over a short period to catch initialization ordering races
        int attempts = 6; // total ~0.3s with 0.05s yields
        float delay = 0.05f;
        int last = -1;
        for (int i = 0; i < attempts; i++)
        {
            int current = 0;
            if (playerShield != null) current = ReadShieldIntFromComponent(playerShield);
            else if (boundShieldComponent != null) current = ReadShieldIntFromComponent(boundShieldComponent);

            if (current != last)
            {
                if (verboseLogging) Debug.Log($"ShieldUI: Reconcile attempt {i} -> {current}");
                UpdateShieldUI(current);
                last = current;
            }

            // if we reached max icons, no need to keep trying
            if (current >= (shieldIcons != null ? shieldIcons.Length : int.MaxValue)) break;

            yield return new WaitForSeconds(delay);
        }

        reconcileCoroutine = null;
    }

    // Called via reflection delegate if we successfully bound an event with signature matching UpdateShieldUI(int)
    private void OnReflectedShieldEvent(int current)
    {
        UpdateShieldUI(current);
    }

    // Unbind any previous bindings and stop polling
    private void Unbind()
    {
        if (playerShield != null)
        {
            // unsubscribe from events
            playerShield.OnShieldChanged -= UpdateShieldUI;
            playerShield = null;
        }

        if (boundShieldComponent != null)
        {
            // try to remove event handler by name (best-effort)
            var evt = boundShieldComponent.GetType().GetEvent("OnShieldChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (evt != null)
            {
                try
                {
                    var handler = Delegate.CreateDelegate(evt.EventHandlerType, this, nameof(OnReflectedShieldEvent));
                    evt.RemoveEventHandler(boundShieldComponent, handler);
                }
                catch { }
            }
            boundShieldComponent = null;
        }

        StopPolling();
        ClearCachedMember();
    }

    public void ClearIcons()
    {
        UpdateShieldUI(0);
    }

    // Attempt to populate shieldIcons from child Image components if the array is empty or contains nulls
    private void TryAutoPopulateIcons()
    {
        if (shieldIcons != null && shieldIcons.Length > 0)
        {
            // quick null check
            bool hasNull = false;
            foreach (var img in shieldIcons) if (img == null) { hasNull = true; break; }
            if (!hasNull) return;
        }

        var imgs = GetComponentsInChildren<Image>(true);
        if (imgs == null || imgs.Length == 0)
        {
            if (verboseLogging) Debug.LogWarning($"ShieldUI: no Image children found to auto-populate shield icons on {gameObject.name}");
            return;
        }

        // Prefer excluding any Image that might belong to this component's background (heuristic: include images that are not on the root)
        var candidateList = new System.Collections.Generic.List<Image>();
        foreach (var im in imgs)
        {
            if (im.gameObject == this.gameObject) continue;
            candidateList.Add(im);
        }

        if (candidateList.Count == 0) candidateList.AddRange(imgs);

        shieldIcons = candidateList.ToArray();
        if (verboseLogging) Debug.Log($"ShieldUI: auto-populated {shieldIcons.Length} shield icon(s) for {gameObject.name}");
    }

    private void StartPolling()
    {
        StopPolling();
        pollCoroutine = StartCoroutine(PollShieldRoutine());
    }

    private void StopPolling()
    {
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }
    }

    private IEnumerator PollShieldRoutine()
    {
        int last = -1;
        while (true)
        {
            int current = 0;
            if (playerShield != null)
                current = ReadShieldIntFromComponent(playerShield);
            else if (boundShieldComponent != null)
                current = ReadShieldIntFromComponent(boundShieldComponent);

            if (current != last)
            {
                UpdateShieldUI(current);
                last = current;
            }

            yield return new WaitForSeconds(0.1f); // poll 10x/sec
        }
    }

    // Try to cache a field/property for faster and consistent reads, using overrideMemberName first
    private void CacheMemberForComponent(Component comp)
    {
        ClearCachedMember();
        if (comp == null) return;

        var t = comp.GetType();

        if (!string.IsNullOrEmpty(overrideMemberName))
        {
            // try exact match first
            var f = t.GetField(overrideMemberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int)) { cachedField = f; LogCacheFound(t, f); return; }
            var p = t.GetProperty(overrideMemberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(int)) { cachedProperty = p; LogCacheFound(t, p); return; }
        }

        // candidate names commonly used
        string[] candidates = new[] { "CurrentShields", "currentShields", "currentShield", "shieldCount", "shields", "shield", "charges", "shieldCharges", "shieldAmount", "shieldValue", "shieldStacks", "current" };

        // prefer named candidates
        foreach (var name in candidates)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int)) { cachedField = f; LogCacheFound(t, f); return; }
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(int)) { cachedProperty = p; LogCacheFound(t, p); return; }
        }

        // final fallback: any int field/property
        var anyField = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(x => x.FieldType == typeof(int));
        if (anyField != null) { cachedField = anyField; LogCacheFound(t, anyField); return; }
        var anyProp = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(x => x.PropertyType == typeof(int) && x.CanRead);
        if (anyProp != null) { cachedProperty = anyProp; LogCacheFound(t, anyProp); return; }

        if (verboseLogging) Debug.LogWarning($"ShieldUI: no int field/property found on component {t.Name}. UI will show 0.");
    }

    private void LogCacheFound(Type t, MemberInfo m)
    {
        if (!verboseLogging) return;
        Debug.Log($"ShieldUI: cached member '{m.Name}' on component {t.Name} (MemberType: {m.MemberType})");
    }

    private void ClearCachedMember()
    {
        cachedField = null;
        cachedProperty = null;
    }

    // Read an integer shield count from a component using the cached member or search otherwise
    private int ReadShieldIntFromComponent(Component comp)
    {
        if (comp == null) return 0;

        if (cachedField != null && cachedField.DeclaringType.IsInstanceOfType(comp))
        {
            try { return (int)cachedField.GetValue(comp); } catch { }
        }
        if (cachedProperty != null && cachedProperty.DeclaringType.IsInstanceOfType(comp))
        {
            try { return (int)cachedProperty.GetValue(comp); } catch { }
        }

        // fallback to previous reflection routine (no caching)
        var t = comp.GetType();

        var fld = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                   .FirstOrDefault(x => x.FieldType == typeof(int) && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
        if (fld != null) return (int)fld.GetValue(comp);

        var prop = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(x => x.PropertyType == typeof(int) && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
        if (prop != null) return (int)prop.GetValue(comp);

        // fallback to any int
        var anyFld = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      .FirstOrDefault(x => x.FieldType == typeof(int));
        if (anyFld != null) return (int)anyFld.GetValue(comp);

        var anyProp = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       .FirstOrDefault(x => x.PropertyType == typeof(int) && x.CanRead);
        if (anyProp != null) return (int)anyProp.GetValue(comp);

        return 0;
    }

    // Update icons (call from event or poll)
    private void UpdateShieldUI(int currentShields)
    {
        if (verboseLogging) Debug.Log($"ShieldUI: UpdateShieldUI -> {currentShields}");

        if (shieldIcons == null || shieldIcons.Length == 0) return;

        currentShields = Mathf.Clamp(currentShields, 0, shieldIcons.Length);

        for (int i = 0; i < shieldIcons.Length; i++)
        {
            if (shieldIcons[i] == null) continue;
            // If sprite overrides are missing, fallback to color tint to indicate filled/empty
            if (filledSprite == null || emptySprite == null)
            {
                // keep existing sprite if present
                shieldIcons[i].gameObject.SetActive(true);
                shieldIcons[i].color = (i < currentShields) ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
            else
            {
                shieldIcons[i].sprite = (i < currentShields) ? filledSprite : emptySprite;
                shieldIcons[i].color = Color.white;
                shieldIcons[i].gameObject.SetActive(true);
            }
        }
    }

    private void RefreshFromBound()
    {
        // ignore prefab/asset references (they live in no loaded scene)
        if (playerShield != null)
        {
            if (!playerShield.gameObject.scene.IsValid())
            {
                if (verboseLogging) Debug.Log($"ShieldUI.RefreshFromBound: ignoring prefab asset playerShield (instanceID {playerShield.GetInstanceID()})");
                return;
            }
            UpdateShieldUI(playerShield.CurrentShields);
            return;
        }

        if (boundShieldComponent != null)
        {
            if (!boundShieldComponent.gameObject.scene.IsValid())
            {
                if (verboseLogging) Debug.Log($"ShieldUI.RefreshFromBound: ignoring prefab asset boundShieldComponent {boundShieldComponent.GetType().Name}");
                return;
            }

            int v = ReadShieldIntFromComponent(boundShieldComponent);
            UpdateShieldUI(v);
            return;
        }

        // no binding -> show zero
        UpdateShieldUI(0);
    }

    private void OnDisable()
    {
        Unbind();
    }
}
