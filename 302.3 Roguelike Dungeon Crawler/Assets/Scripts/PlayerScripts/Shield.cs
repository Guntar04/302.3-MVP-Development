using System;
using System.Collections;
using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("Shield Settings")]
    public int maxShields = 3;
    public float regenDelay = 5f;      // seconds after last shield loss before regen starts
    public float regenInterval = 1f;   // seconds per shield restored while regenerating

    public int CurrentShields { get; private set; }

    public event Action<int> OnShieldChanged;

    private Coroutine regenCoroutine;

    private void Awake()
    {
        CurrentShields = maxShields;
        // immediate notify (if anyone already subscribed)
        OnShieldChanged?.Invoke(CurrentShields);

        // also notify one frame later to catch listeners that subscribe during the same frame
        StartCoroutine(NotifyOnNextFrame(CurrentShields));

        Debug.Log($"Shield.Awake -> instanceID {GetInstanceID()} CurrentShields={CurrentShields}");
    }

    private void Start()
    {
        // Failsafe: tell UIManager about this player instance if it's available.
        // This ensures the ShieldUI binds to the runtime instance even if the
        // usual BindPlayer call was missed.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.BindPlayer(this.gameObject);
            Debug.Log($"Shield.Start -> requested UIManager.BindPlayer instanceID {GetInstanceID()}");
        }
    }

    public bool TryConsumeShield()
    {
        if (CurrentShields <= 0) return false;

        CurrentShields = Mathf.Max(0, CurrentShields - 1);
        Debug.Log($"Shield.TryConsumeShield -> instanceID {GetInstanceID()} CurrentShields now {CurrentShields}");
        OnShieldChanged?.Invoke(CurrentShields);

        // restart regen timer
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenAfterDelay());

        return true;
    }

    public void AddShields(int amount)
    {
        if (amount <= 0) return;
        CurrentShields = Mathf.Clamp(CurrentShields + amount, 0, maxShields);
        Debug.Log($"Shield.AddShields -> instanceID {GetInstanceID()} CurrentShields now {CurrentShields}");
        OnShieldChanged?.Invoke(CurrentShields);
    }

    private IEnumerator RegenAfterDelay()
    {
        yield return new WaitForSeconds(regenDelay);

        while (CurrentShields < maxShields)
        {
            yield return new WaitForSeconds(regenInterval);
            CurrentShields = Mathf.Clamp(CurrentShields + 1, 0, maxShields);
            OnShieldChanged?.Invoke(CurrentShields);
        }

        regenCoroutine = null;
    }

    private IEnumerator NotifyOnNextFrame(int value)
    {
        yield return null;
        OnShieldChanged?.Invoke(value);
    }

    /// <summary>
    /// Force reset (useful when entering a new floor / respawn).
    /// </summary>
    public void ResetShields()
    {
        CurrentShields = maxShields;
        if (regenCoroutine != null) { StopCoroutine(regenCoroutine); regenCoroutine = null; }
        Debug.Log($"Shield.ResetShields -> instanceID {GetInstanceID()} CurrentShields now {CurrentShields}");
        OnShieldChanged?.Invoke(CurrentShields);
    }
}
