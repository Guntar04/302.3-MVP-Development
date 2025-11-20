using UnityEngine;

[System.Serializable]
public class EquipmentStats
{
    public Loot.EquipmentType equipmentType;
    public int attackPower;    // for swords
    public float moveSpeed; // for sword
    public int defense;        // for armour

    public override string ToString()
    {
        if (equipmentType == Loot.EquipmentType.Sword)
            return $"Sword Attack:{attackPower}";
        else
            return $"Armour Defense:{defense}";
    }
}
