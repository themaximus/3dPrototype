using UnityEngine;
using System.Collections.Generic;

public class UI_InventoryManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("������ �� '����' ��������� (������ �� ������)")]
    [SerializeField] private InventorySystem inventorySystem;

    [Tooltip("������ UI-�����, ������� �� �������")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Containers")]
    [Tooltip("������ � GridLayoutGroup ��� ��������� ���������")]
    [SerializeField] private Transform mainInventoryContainer;

    [Tooltip("������ � GridLayoutGroup ��� ������� ������")]
    [SerializeField] private Transform quickSlotContainer;

    [Header("Toggle Settings")]
    [Tooltip("������� ��� ��������/�������� ���������")]
    public KeyCode toggleKey = KeyCode.I;

    // ������ ��� �������� ������ �� ��������� UI-�����
    private List<UI_InventorySlot> mainSlotsUI = new List<UI_InventorySlot>();
    private List<UI_InventorySlot> quickSlotsUI = new List<UI_InventorySlot>();

    private CanvasGroup inventoryCanvasGroup;

    void Awake()
    {
        inventoryCanvasGroup = GetComponent<CanvasGroup>();
        if (inventoryCanvasGroup == null)
        {
            inventoryCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // ��������� �� ��������� ������ ���� �����
        ToggleInventory(false);
    }

    void Start()
    {
        if (inventorySystem == null || slotPrefab == null)
        {
            Debug.LogError("UI_InventoryManager: �� ��� ������ ��������� � ����������!", this);
            return;
        }

        // --- ������� ������ ---
        // 1. ������� UI-�����
        CreateSlotGrid(inventorySystem.MainInventory, mainInventoryContainer, mainSlotsUI, false);
        CreateSlotGrid(inventorySystem.QuickSlots, quickSlotContainer, quickSlotsUI, true);

        // 2. ������������� �� ������� "�����"
        // ��� - ������ �����������.
        inventorySystem.OnMainInventorySlotUpdated += UpdateMainSlot;
        inventorySystem.OnQuickSlotUpdated += UpdateQuickSlot;
        // -------------------------
    }

    void Update()
    {
        // ������ ��������/�������� ���������
        if (Input.GetKeyDown(toggleKey))
        {
            // ���� ��������� ������ - ���������, � ��������
            ToggleInventory(inventoryCanvasGroup.alpha == 0);
        }
    }

    /// <summary>
    /// �������� ��� ��������� UI ���������
    /// </summary>
    public void ToggleInventory(bool show)
    {
        if (show)
        {
            inventoryCanvasGroup.alpha = 1;
            inventoryCanvasGroup.interactable = true;
            inventoryCanvasGroup.blocksRaycasts = true;

            // �������� ������, ����� ����������������� � UI
            // (��� ��� � ����� GameManager)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            inventoryCanvasGroup.alpha = 0;
            inventoryCanvasGroup.interactable = false;
            inventoryCanvasGroup.blocksRaycasts = false;

            // ���������� ���������� ������
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// ������������� ������� ����� UI-������
    /// </summary>
    private void CreateSlotGrid(IReadOnlyList<InventorySlot> inventoryData, Transform container, List<UI_InventorySlot> uiList, bool isQuickSlot)
    {
        for (int i = 0; i < inventoryData.Count; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, container);
            UI_InventorySlot uiSlot = slotGO.GetComponent<UI_InventorySlot>();

            // "��������" UI-����, ����� ��� ��� ������ ������
            uiSlot.Initialize(inventorySystem, inventoryData[i], i, isQuickSlot);

            uiList.Add(uiSlot);
        }
    }

    // --- ������, ������� ���������� ��������� ---

    /// <summary>
    /// ���� ����� ���������� �������� OnMainInventorySlotUpdated �� InventorySystem
    /// </summary>
    private void UpdateMainSlot(int index)
    {
        if (index < mainSlotsUI.Count)
        {
            mainSlotsUI[index].UpdateVisuals();
        }
    }

    /// <summary>
    /// ���� ����� ���������� �������� OnQuickSlotUpdated �� InventorySystem
    /// </summary>
    private void UpdateQuickSlot(int index)
    {
        if (index < quickSlotsUI.Count)
        {
            quickSlotsUI[index].UpdateVisuals();
        }
    }

    /// <summary>
    /// ����� ���������� �� �������, ����� ������ ������������
    /// </summary>
    void OnDestroy()
    {
        if (inventorySystem != null)
        {
            inventorySystem.OnMainInventorySlotUpdated -= UpdateMainSlot;
            inventorySystem.OnQuickSlotUpdated -= UpdateQuickSlot;
        }
    }
}