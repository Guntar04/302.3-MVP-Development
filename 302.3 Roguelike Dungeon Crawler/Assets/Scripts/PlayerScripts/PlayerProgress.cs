using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;

public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance { get; private set; }

    public static bool HasSaved { get; private set; } = false;
    public static int health = 10;
    public static int maxHealth = 10;
    public static int shieldCount = 0;

    public static EquipmentStats savedWeaponStats;
    public static EquipmentStats savedChestplateStats;
    public static EquipmentStats savedHelmetStats;
    public static EquipmentStats savedPantsStats;
    public static EquipmentStats savedBootsStats;
    public static EquipmentStats savedShieldStats;




    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveFromInstance(PlayerController pc)
    {
        if (pc == null) return;
        health = pc.health;
        maxHealth = pc.maxHealth;
        shieldCount = TryGetShieldCount(pc.gameObject);
        HasSaved = true;
    }

    public void ApplyToInstance(PlayerController pc)
    {
        if (pc == null || !HasSaved) return;
        pc.maxHealth = Mathf.Clamp(maxHealth, 1, 20);
        pc.health = Mathf.Clamp(health, 0, pc.maxHealth);
        pc.UpdatePlayerHealth();
        TryApplyShieldCount(pc.gameObject, shieldCount);
    }

    public static void SaveFrom(PlayerController pc)
    {
        if (Instance != null) Instance.SaveFromInstance(pc);
        else
        {
            if (pc == null) return;
            health = pc.health;
            maxHealth = pc.maxHealth;
            shieldCount = TryGetShieldCountStatic(pc.gameObject);
            HasSaved = true;
            savedWeaponStats = pc.equippedWeaponStats;
            savedChestplateStats = pc.equippedChestplateStats;
            savedHelmetStats = pc.equippedHelmetStats;
            savedPantsStats = pc.equippedPantsStats;
            savedBootsStats = pc.equippedBootsStats;
            savedShieldStats = pc.equippedShieldStats;

        }
    }

    public static void ApplyTo(PlayerController pc)
    {
        if (pc == null || !HasSaved) return;
        if (Instance != null) { Instance.ApplyToInstance(pc); return; }
        pc.maxHealth = Mathf.Clamp(maxHealth, 1, 20);
        pc.health = Mathf.Clamp(health, 0, pc.maxHealth);
        pc.UpdatePlayerHealth();
        TryApplyShieldCountStatic(pc.gameObject, shieldCount);
       if (savedWeaponStats != null)
    pc.EquipItemStats(savedWeaponStats, Loot.EquipmentType.Sword);

if (savedChestplateStats != null)
    pc.EquipItemStats(savedChestplateStats, Loot.EquipmentType.Chestplate);

if (savedHelmetStats != null)
    pc.EquipItemStats(savedHelmetStats, Loot.EquipmentType.Helmet);

if (savedPantsStats != null)
    pc.EquipItemStats(savedPantsStats, Loot.EquipmentType.Pants);

if (savedBootsStats != null)
    pc.EquipItemStats(savedBootsStats, Loot.EquipmentType.Boots);

if (savedShieldStats != null)
    pc.EquipItemStats(savedShieldStats, Loot.EquipmentType.Shield);

    }

    // Prefer field/property names that reference "shield"/"armor"/"charge"/"count"
    private int TryGetShieldCount(GameObject go) => TryGetShieldCountStatic(go);
    private static int TryGetShieldCountStatic(GameObject go)
    {
        if (go == null) return 0;
        var candidate = go.GetComponent("Shield") ?? go.GetComponent("ShieldController") ?? go.GetComponent("Armor");
        if (candidate == null)
        {
            // fallback: search components for likely int field/property
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var t = comp.GetType();
                // prefer named fields/properties
                var f = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .FirstOrDefault(x => x.FieldType == typeof(int) && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
                if (f != null) return (int)f.GetValue(comp);
                var p = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .FirstOrDefault(x => x.PropertyType == typeof(int) && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
                if (p != null) return (int)p.GetValue(comp);
            }

            // final fallback: any int field/property on any component
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var t = comp.GetType();
                var fAny = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(x => x.FieldType == typeof(int));
                if (fAny != null) return (int)fAny.GetValue(comp);
                var pAny = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(x => x.PropertyType == typeof(int));
                if (pAny != null) return (int)pAny.GetValue(comp);
            }

            return 0;
        }

        var tt = candidate.GetType();
        // prefer named fields/properties
        var field = tt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      .FirstOrDefault(x => x.FieldType == typeof(int) && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
        if (field != null) return (int)field.GetValue(candidate);
        var prop = tt.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     .FirstOrDefault(x => x.PropertyType == typeof(int) && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
        if (prop != null) return (int)prop.GetValue(candidate);

        // fallback to any int
        var anyField = tt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .FirstOrDefault(x => x.FieldType == typeof(int));
        if (anyField != null) return (int)anyField.GetValue(candidate);
        var anyProp = tt.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(x => x.PropertyType == typeof(int));
        if (anyProp != null) return (int)anyProp.GetValue(candidate);

        return 0;
    }

    private void TryApplyShieldCount(GameObject go, int value) => TryApplyShieldCountStatic(go, value);
    private static void TryApplyShieldCountStatic(GameObject go, int value)
    {
        if (go == null) return;

        // try direct known component names first
        var candidate = go.GetComponent("Shield") ?? go.GetComponent("ShieldController") ?? go.GetComponent("Armor");
        if (candidate == null)
        {
            // search all components for a likely field/property
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var t = comp.GetType();
                // try named fields/properties first
                var f = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .FirstOrDefault(x => x.FieldType == typeof(int) && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
                if (f != null) { f.SetValue(comp, value); TryInvokeShieldUpdate(comp); return; }
                var p = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .FirstOrDefault(x => x.PropertyType == typeof(int) && x.CanWrite && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
                if (p != null) { p.SetValue(comp, value); TryInvokeShieldUpdate(comp); return; }
            }

            // final fallback: any int field/property
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var t = comp.GetType();
                var fAny = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(x => x.FieldType == typeof(int));
                if (fAny != null) { fAny.SetValue(comp, value); TryInvokeShieldUpdate(comp); return; }
                var pAny = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(x => x.PropertyType == typeof(int) && x.CanWrite);
                if (pAny != null) { pAny.SetValue(comp, value); TryInvokeShieldUpdate(comp); return; }
            }

            return;
        }

        var tt = candidate.GetType();
        // named field/property
        var field = tt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      .FirstOrDefault(x => x.FieldType == typeof(int) && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
        if (field != null) { field.SetValue(candidate, value); TryInvokeShieldUpdate(candidate); return; }
        var prop = tt.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     .FirstOrDefault(x => x.PropertyType == typeof(int) && x.CanWrite && (x.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0 || x.Name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0));
        if (prop != null) { prop.SetValue(candidate, value); TryInvokeShieldUpdate(candidate); return; }

        // fallback any int
        var anyField = tt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .FirstOrDefault(x => x.FieldType == typeof(int));
        if (anyField != null) { anyField.SetValue(candidate, value); TryInvokeShieldUpdate(candidate); return; }
        var anyProp = tt.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(x => x.PropertyType == typeof(int) && x.CanWrite);
        if (anyProp != null) { anyProp.SetValue(candidate, value); TryInvokeShieldUpdate(candidate); return; }

        // last resort: try SetShields(int)
        var setMethod = tt.GetMethod("SetShields", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setMethod != null) { setMethod.Invoke(candidate, new object[] { value }); TryInvokeShieldUpdate(candidate); return; }
    }

    // try to call common update/refresh methods on the shield component so UI refreshes
    private static void TryInvokeShieldUpdate(object comp)
    {
        if (comp == null) return;
        var t = comp.GetType();
        string[] methodNames = new[]
        {
            "RefreshUI", "Refresh", "UpdateUI", "UpdateShieldDisplay", "UpdateShield", "OnShieldChanged",
            "OnShieldsChanged", "RebuildUI", "SetShieldsVisual", "SyncUI"
        };

        foreach (var name in methodNames)
        {
            var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null)
            {
                try { m.Invoke(comp, null); } catch { }
                return;
            }
        }

        // try parameterized "UpdateShields(int)" style
        foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (m.Name.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0 && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int))
            {
                try { m.Invoke(comp, new object[] { shieldCount }); } catch { }
                return;
            }
        }

        // if nothing found, also attempt to send a Unity message (if component inherits MonoBehaviour)
        var mb = comp as MonoBehaviour;
        if (mb != null)
        {
            try { mb.SendMessage("OnShieldChanged", SendMessageOptions.DontRequireReceiver); } catch { }
            try { mb.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver); } catch { }
        }
    }

    // Clear saved progress so a new game starts fresh
    public static void ResetProgress()
    {
        HasSaved = false;
        health = 10;
        maxHealth = 10;
        shieldCount = 0;
        // clear any runtime singleton instance fields too
        if (Instance != null)
        {
            Instance = null; // allow recreation if needed
        }

        // clear player-dead flag if set
        PlayerController.SetPlayerDead(false);
    }
}
