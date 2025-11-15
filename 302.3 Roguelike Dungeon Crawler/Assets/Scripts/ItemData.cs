using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    public EquipmentStats equipmentStats;   // link the stats object here

    [HideInInspector] public Loot originalLoot; // optional reference
}
