using UnityEngine;

/// <summary>
/// ������� �����, �� MonoBehaviour, ������� ������������
/// ���� ���� � ���������.
/// </summary>
[System.Serializable] // ��� ��������� Unity ��������� � ���������� ���� ����� � ����������
public class InventorySlot
{
    // ������ �� ������ �������� (ScriptableObject)
    public ItemData itemData;
    // ������� ���������� ��������� � ���� �����
    public int quantity;

    /// <summary>
    /// ����������� ��� �������� ������� �����
    /// </summary>
    public InventorySlot()
    {
        itemData = null;
        quantity = 0;
    }

    /// <summary>
    /// ����������� ��� �������� ����� � ���������
    /// </summary>
    public InventorySlot(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
    }

    /// <summary>
    /// ��������: ���������, ���� �� ����
    /// </summary>
    public bool IsEmpty()
    {
        return itemData == null || quantity <= 0;
    }

    /// <summary>
    /// ������� ����
    /// </summary>
    public void ClearSlot()
    {
        itemData = null;
        quantity = 0;
    }

    /// <summary>
    /// ��������� ������������ ���������� � ����� �����
    /// </summary>
    public void AddQuantity(int amount)
    {
        quantity += amount;
    }

    /// <summary>
    /// ������� ������������ ���������� �� ����� �����
    /// </summary>
    public void RemoveQuantity(int amount)
    {
        quantity -= amount;
        if (quantity <= 0)
        {
            ClearSlot();
        }
    }
}