using UnityEngine;

/// <summary>
/// ���������� ������� ���� ���������.
/// </summary>
public enum ItemType
{
    Default,
    Weapon,
    Consumable,
    Equipment
}

[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string itemName;
    [TextArea(3, 5)] // ������� ���� �������� �������� � ����������
    public string description;
    public Sprite itemIcon;
    public ItemType itemType = ItemType.Default;

    [Header("World")]
    // ������, ������� ����� ���������� ��� ������������
    // � ����� ������� ������ ���� Rigidbody � Collider
    public GameObject worldPrefab;

    [Header("Stacking")]
    public bool isStackable = false;
    // maxStackSize ����� ��������������, ���� isStackable = false
    public int maxStackSize = 1;

    /// <summary>
    /// ����������� ����� ��� "�������������" �������� (��������, �� ������� ��� � ���������).
    /// ���������� (��� Consumable) ������ ��� ��������������.
    /// </summary>
    public virtual void Use()
    {
        // �� ��������� - ������ �� ������
        // --- ����������� ����� ---
        Debug.Log("Using: " + itemName);
    }
}