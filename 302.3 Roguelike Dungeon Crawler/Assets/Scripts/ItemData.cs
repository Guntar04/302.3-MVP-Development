using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;      
    public Sprite icon;          
    public ItemType itemType;    

    [Header("Stats")]
    public int shieldBonus;  // e.g. +20 shield from armor
    public int healthBonus;  // optional, for items that boost HP
}

