using UnityEngine;

/// <summary>
/// ����������, � ����� ���� ���������� ���������� �������
/// </summary>
public enum EquipmentSlot
{
    Head,
    Chest,
    Legs,
    Feet,
    // ������ �������� ��� ������: Hands, Shoulders, Amulet...
}

[CreateAssetMenu(fileName = "New Equipment Item", menuName = "Inventory/Equipment Item")]
public class EquipmentItemData : ItemData
{
    [Header("Equipment Settings")]
    public EquipmentSlot slot;

    [Tooltip("�� ������� ���� ������� ������� �������� ���� (� % ��� ��.)")]
    public int defenseModifier;

    // ���� ����� �������� � ������ �����, ��������, 
    // public float speedModifier;

    public override void Use()
    {
        // "������������" = "�����������"
        Debug.Log("Equipping " + itemName + " to " + slot);

        // ����� ����� ����� ����� InventorySystem.Equip(this)
        base.Use();
    }

    private void OnValidate()
    {
        // ������������� ������������� ���������� ��� ��������
        itemType = ItemType.Equipment;
    }
}