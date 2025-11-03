using UnityEngine;

[System.Serializable]
public class EquipmentStats
{
    public Loot.EquipmentType equipmentType;
    public int attackPower;    // for swords
    public float attackSpeed;  // for swords
    public int defense;        // for armour

    public override string ToString()
    {
        if (equipmentType == Loot.EquipmentType.Sword)
            return $"Sword AP:{attackPower} SPD:{attackSpeed:F2}";
        else
            return $"Armour DEF:{defense}";
    }
}
