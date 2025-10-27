using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Item", menuName = "Inventory/Weapon Item")]
public class WeaponItemData : ItemData
{
    [Header("Weapon Settings")]

    [Tooltip("������ �� ScriptableObject �� ������� (����, ���������...)")]
    // ��� ������ �� ���� ��� ������������ WeaponData.cs
    public WeaponData weaponStats;

    [Tooltip("������, ������� ����� ���������� � ����� � ������ ��� ����������")]
    // � ����� ������� ������ ���� ���� HandController/WeaponController
    public GameObject handModelPrefab;

    // ���� ����� "��������������" ������������
    public override void Use()
    {
        // "������������" ��� ������ � ��������� - ������ "�����������"
        // (����� �� ������� ���� ������)
        Debug.Log("Equipping " + itemName);

        // base.Use(); // ����� ������� ������������ �����, ���� �����
    }

    /// <summary>
    /// ���� ����� ���������� �������������, ����� �� �������� �����
    /// ��� ����� ������ �����������.
    /// </summary>
    private void OnValidate()
    {
        // ������������� ������������� ���������� ��� ��������
        itemType = ItemType.Weapon;
    }
}