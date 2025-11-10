using UnityEngine;

[CreateAssetMenu(fileName = "New Loot", menuName = "Loot")]
public class Loot : ScriptableObject
{
    [Header("Loot")]
    public Sprite lootSprite;
    public string lootName;
    public int dropChance;

    // --- NEW: type & equipment stat ranges ---
    public enum LootCategory { Consumable, Equipment }
    public enum EquipmentType
{
    Sword,
    Chestplate,
    Helmet,
    Pants,
    Boots,
    Shield
}


    [Header("Type")]
    public LootCategory category = LootCategory.Equipment;
    public EquipmentType equipmentType = EquipmentType.Sword;

    [Header("Equipment stat ranges (used if category == Equipment)")]
    public int minAttack = 1;
    public int maxAttack = 5;
    public float minSpeed = 0.5f;
    public float maxSpeed = 1.5f;
    public int minDefense = 1;
    public int maxDefense = 5;
    public float minAttackSpeed = 0f;
    public float maxAttackSpeed = 3f;
    // --- end new fields ---

    public Loot(string lootName, int dropChance)
    {
        this.lootName = lootName;
        this.dropChance = dropChance;
    }
}
