using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the Player. Manages shield count, blocking and timed regeneration.
/// </summary>
public class Shield : MonoBehaviour
{
    [Header("Shield Settings")]
    public int maxShields = 3;
    public float regenDelay = 5f;      // seconds after last shield loss before regen starts
    public float regenInterval = 1f;   // seconds per shield restored while regenerating

    public int CurrentShields { get; private set; }

    // event(int newCount)
    public event Action<int> OnShieldChanged;

    private Coroutine regenCoroutine;

    private void Awake()
    {
        CurrentShields = maxShields;
        OnShieldChanged?.Invoke(CurrentShields);
    }

    /// <summary>
    /// Try to consume a shield. Returns true if a shield was consumed (damage blocked).
    /// </summary>
    public bool TryConsumeShield()
    {
        if (CurrentShields <= 0) return false;

        CurrentShields = Mathf.Max(0, CurrentShields - 1);
        OnShieldChanged?.Invoke(CurrentShields);

        // restart regen timer
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenAfterDelay());

        return true;
    }

    /// <summary>
    /// Immediately add shields (clamped to max).
    /// </summary>
    public void AddShields(int amount)
    {
        if (amount <= 0) return;
        CurrentShields = Mathf.Clamp(CurrentShields + amount, 0, maxShields);
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

    /// <summary>
    /// Force reset (useful when entering a new floor / respawn).
    /// </summary>
    public void ResetShields()
    {
        CurrentShields = maxShields;
        if (regenCoroutine != null) { StopCoroutine(regenCoroutine); regenCoroutine = null; }
        OnShieldChanged?.Invoke(CurrentShields);
    }
}
