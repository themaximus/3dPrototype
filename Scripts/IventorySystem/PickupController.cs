using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("������ �� ������� ������ ������ (FPS ������)")]
    public Camera playerCamera;

    [Tooltip("������ �� '����' ���������. ������ ������� �������������.")]
    private InventorySystem inventorySystem;

    [Header("Interaction Settings")]
    [Tooltip("��� ������ ����� ����� ��������� ��������")]
    public float pickupDistance = 3f;

    [Tooltip("������� ��� ������� �������� � ��������� (E - ��� ������ ���������� ��������)")]
    public KeyCode pickupKey = KeyCode.F;

    void Awake()
    {
        // ������������� ������� InventorySystem �� ���� �� �������
        inventorySystem = GetComponent<InventorySystem>();
    }

    void Start()
    {
        // ��������, ��������� �� ������
        if (playerCamera == null)
        {
            // ��������� ����� �� �������������
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                // ���� � ���� ��� Main Camera, ���������� ������ �� PlayerController
                // (����������� �� ����� PlayerController.cs)
                FirstPersonController controller = GetComponent<FirstPersonController>();
                if (controller != null && controller.cameraTransform != null)
                {
                    playerCamera = controller.cameraTransform.GetComponent<Camera>();
                }
            }

            if (playerCamera == null)
            {
                Debug.LogError("PlayerInteractor: ������ �� �������! ������� 'playerCamera' ������� � ����������.", this);
                this.enabled = false; // ��������� ������, ����� �� ���� ������
            }
        }
    }

    void Update()
    {
        // ��������� ������� ������� �������
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupItem();
        }
    }

    /// <summary>
    /// ������� ��� �� ������ ������ � �������� ��������� �������
    /// </summary>
    private void TryPickupItem()
    {
        // ������� ��� �� ������ ������
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        // ������� ��� �� ��������� 'pickupDistance'
        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            // ���������, ���� �� �� ������� ��������� ItemPickup
            ItemPickup itemPickup = hit.collider.GetComponent<ItemPickup>();

            if (itemPickup != null)
            {
                // ���������, ����� �� ���� ������� ������ ������ � ���������
                if (itemPickup.isNonInventoryItem)
                {
                    // (���� ����� �������� ��������� "���� ������� ������ �����")
                    return;
                }

                // �������� �������� ������� � "����" ���������
                bool successfullyAdded = inventorySystem.AddItem(itemPickup.itemData, itemPickup.quantity);

                if (successfullyAdded)
                {
                    // ���� ����� �������, � ������� �������� - ���������� ������ � ����
                    Destroy(hit.collider.gameObject);
                }
                else
                {
                    // (���� ����� �������� ��������� "��������� �����")
                    Debug.Log("�� ������� ������� " + itemPickup.itemData.itemName + ". ��������� �����!");
                }
            }
        }
    }
}