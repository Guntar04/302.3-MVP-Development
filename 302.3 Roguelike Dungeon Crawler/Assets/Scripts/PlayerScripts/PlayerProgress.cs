using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small example that saves/restores basic player state across floors using LevelManager events.
/// Adapt the inventory/xp lines to integrate with your real systems.
/// </summary>
public class PlayerProgress : MonoBehaviour
{
    // saved state
    [HideInInspector] public int savedHealth;
    [HideInInspector] public int savedXP;
    [HideInInspector] public List<string> savedItems = new List<string>();

    // references (set or auto-find)
    PlayerController pc;
    // replace 'Inventory' below with your actual inventory type if you have one
    // Inventory inventory;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        // inventory = GetComponent<Inventory>();
        StartCoroutine(RegisterWhenReady());
    }

    IEnumerator RegisterWhenReady()
    {
        // ensure LevelManager is initialized (it is DontDestroyOnLoad)
        while (LevelManager.Instance == null)
            yield return null;

        LevelManager.Instance.OnBeforeFloorUnload.AddListener(OnBeforeFloorUnload);
        LevelManager.Instance.OnAfterFloorLoad.AddListener(OnAfterFloorLoad);
    }

    // This method matches UnityEvent<int> signature (floor number)
    public void OnBeforeFloorUnload(int leavingFloor)
    {
        // save health (direct field). If your PlayerController stores health differently, adapt.
        if (pc != null)
            savedHealth = pc.health;

        // save xp - adapt to your XP implementation
        // savedXP = GetComponent<PlayerXP>()?.currentXP ?? 0;

        // save inventory (example: convert items to string IDs)
        savedItems.Clear();
        // if (inventory != null) foreach (var it in inventory.GetItems()) savedItems.Add(it.id);

        Debug.Log($"PlayerProgress: saved health={savedHealth} xp={savedXP} items={savedItems.Count}");
    }

    public void OnAfterFloorLoad(int newFloor)
    {
        // restore health
        if (pc != null)
        {
            pc.health = savedHealth;
            // make sure any UI / healthbar sync method is called if needed
        }

        // restore xp
        // var xpComp = GetComponent<PlayerXP>();
        // if (xpComp != null) xpComp.currentXP = savedXP;

        // restore inventory
        // if (inventory != null) { inventory.Clear(); foreach (var id in savedItems) inventory.AddById(id); }

        Debug.Log($"PlayerProgress: restored health={savedHealth} xp={savedXP} items={savedItems.Count} for floor {newFloor}");
    }
}
