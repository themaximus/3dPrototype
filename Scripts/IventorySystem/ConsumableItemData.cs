using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable Item", menuName = "Inventory/Consumable Item")]
public class ConsumableItemData : ItemData
{
    [Header("Consumable Settings")]
    [Tooltip("������� �������� ��������������� ���� �������")]
    public int healthToRestore = 0;

    [Tooltip("������� ������ ������� ���� ������� (���� � ���� ����� ������� ������)")]
    public int hungerToRestore = 0;

    // ���� ����� �������� ����� ������ �������:
    // public float speedBoostDuration = 0f;
    // public int manaToRestore = 0;

    /// <summary>
    /// �������������� ������� ����� Use()
    /// </summary>
    public override void Use()
    {
        // "������������" = "���������"
        Debug.Log("Consuming " + itemName + ". Restoring " + healthToRestore + " health.");

        // ����� ����� ����� ������ ������ StatController ������
        // � ����� Player.GetComponent<StatController>().Heal(healthToRestore);

        // base.Use(); // �������� ������������ ����� �� �����������,
        // ��� ��� �� ��������� �������� ��� ������
    }

    private void OnValidate()
    {
        // ������������� ������������� ���������� ��� ��������
        itemType = ItemType.Consumable;
        // ����������� �������� ����� ������ ���������
        isStackable = true;
        if (maxStackSize <= 1)
        {
            maxStackSize = 10; // ��������� �������� �� ���������
        }
    }
}