using UnityEngine;

// ������ �������� � ���� ������ ����� Rigidbody � Collider
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Data")]
    // ������ �� ScriptableObject ��������
    public ItemData itemData;
    // ���������� � ���� "�����"
    public int quantity = 1;

    [Header("Pickup Settings")]
    [Tooltip("���� true, ������� ������ ����� ��������� � ��������� (������ ��������� �������)")]
    public bool isNonInventoryItem = false;

    // --- ������ ���� ---
    // ���� ��� ������ ������ �� �������,
    // ����� ���� ������������ PickupController.cs ��� ��� ��������� �������.
    private const string PICKUPABLE_TAG = "Pickupable";

    void Awake()
    {
        // ��������, ��� � ������� ���������� ��� ��� ������ PickupController
        if (!gameObject.CompareTag(PICKUPABLE_TAG))
        {
            gameObject.tag = PICKUPABLE_TAG;
            Debug.LogWarning($"�� {gameObject.name} �� ���� ���� '{PICKUPABLE_TAG}'. �������� �������������. �������, ��� ���� PickupController ���� ���� ���.");
        }
    }

    /// <summary>
    /// ���� ����� ���������� �� InventorySystem, ����� ������� �������������
    /// </summary>
    public void SetItem(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;

        // ���� � �������� ��� ����������� 3D ������, ����� �����������
        // ���������� ������ ��� ���-�� ���, �� ���� ��� �� �����������
    }
}