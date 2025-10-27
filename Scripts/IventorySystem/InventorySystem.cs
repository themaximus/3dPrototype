using UnityEngine;
using System;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("������ ��������� ���������")]
    public int mainInventorySize = 20;
    [Tooltip("������ ������ ������� ������")]
    public int quickSlotsSize = 4;

    [Header("Inventory Data")]
    [SerializeField]
    private List<InventorySlot> mainInventory = new List<InventorySlot>();

    [SerializeField]
    private List<InventorySlot> quickSlots = new List<InventorySlot>();

    [Header("Drop Settings")]
    [Tooltip("�����, �� ������� ����� ������������� ��������. ������ - �������� ������ ������.")]
    public Transform dropPoint;

    // --- ������� ---
    public event Action<int> OnMainInventorySlotUpdated;
    public event Action<int> OnQuickSlotUpdated;

    public IReadOnlyList<InventorySlot> MainInventory => mainInventory;
    public IReadOnlyList<InventorySlot> QuickSlots => quickSlots;


    void Awake()
    {
        InitializeInventory(mainInventory, mainInventorySize);
        InitializeInventory(quickSlots, quickSlotsSize);

        if (dropPoint == null)
        {
            dropPoint = transform;
        }
    }

    private void InitializeInventory(List<InventorySlot> list, int size)
    {
        for (int i = 0; i < size; i++)
        {
            list.Add(new InventorySlot());
        }
    }

    public bool AddItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        if (item.isStackable)
        {
            int remainingQuantity = AddToExistingStacks(mainInventory, item, quantity, OnMainInventorySlotUpdated);
            if (remainingQuantity <= 0) return true;

            remainingQuantity = AddToExistingStacks(quickSlots, item, remainingQuantity, OnQuickSlotUpdated);
            if (remainingQuantity <= 0) return true;

            quantity = remainingQuantity;
        }

        int emptySlotIndex = FindEmptySlot(mainInventory);
        if (emptySlotIndex != -1)
        {
            mainInventory[emptySlotIndex] = new InventorySlot(item, quantity);
            OnMainInventorySlotUpdated?.Invoke(emptySlotIndex);
            return true;
        }

        emptySlotIndex = FindEmptySlot(quickSlots);
        if (emptySlotIndex != -1)
        {
            quickSlots[emptySlotIndex] = new InventorySlot(item, quantity);
            OnQuickSlotUpdated?.Invoke(emptySlotIndex);
            return true;
        }

        Debug.Log("��� ����� � ���������!");
        return false;
    }

    // --- ��������� (1) ---
    // ������ ����� ��������� bool-����� ������ ����� �������
    /// <summary>
    /// ���������� ��� "�������" �������� (���������� UI Drag & Drop)
    /// </summary>
    public void MoveItem(bool fromIsQuick, int fromIndex, bool toIsQuick, int toIndex)
    {
        // ����������, � ������ �������� ��������
        List<InventorySlot> fromList = fromIsQuick ? quickSlots : mainInventory;
        List<InventorySlot> toList = toIsQuick ? quickSlots : mainInventory;

        // TODO: �������� ������ ������� ������

        // ������� ������ "�����" (������)
        InventorySlot fromSlot = fromList[fromIndex];
        InventorySlot toSlot = toList[toIndex];

        fromList[fromIndex] = toSlot;
        toList[toIndex] = fromSlot;

        // ���������� UI �� ��������� *�����* ������
        NotifySlotChange(fromList, fromIndex);
        NotifySlotChange(toList, toIndex);
    }

    // --- ��������� (2) ---
    // ������ ����� ��������� bool-���� ������ ������
    /// <summary>
    /// ����������� ������� �� ����� (���������� UI Drag & Drop)
    /// </summary>
    public void DropItem(bool fromIsQuick, int fromIndex)
    {
        // ����������, � ����� ������� ��������
        List<InventorySlot> fromList = fromIsQuick ? quickSlots : mainInventory;

        InventorySlot slot = fromList[fromIndex];
        if (slot.IsEmpty()) return;

        ItemData itemToDrop = slot.itemData;
        int quantityToDrop = slot.quantity;

        if (itemToDrop.worldPrefab != null)
        {
            GameObject droppedItemObj = Instantiate(itemToDrop.worldPrefab, dropPoint.position, dropPoint.rotation);

            ItemPickup pickup = droppedItemObj.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.SetItem(itemToDrop, quantityToDrop);
            }
        }

        slot.ClearSlot();
        NotifySlotChange(fromList, fromIndex);
    }

    // --- ��������������� ������ ---

    private int FindEmptySlot(List<InventorySlot> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].IsEmpty())
            {
                return i;
            }
        }
        return -1;
    }

    private int AddToExistingStacks(List<InventorySlot> list, ItemData item, int quantity, Action<int> updateEvent)
    {
        for (int i = 0; i < list.Count; i++)
        {
            InventorySlot slot = list[i];
            if (!slot.IsEmpty() && slot.itemData == item && slot.quantity < item.maxStackSize)
            {
                int spaceLeft = item.maxStackSize - slot.quantity;
                int amountToAdd = Mathf.Min(quantity, spaceLeft);

                slot.AddQuantity(amountToAdd);
                quantity -= amountToAdd;

                updateEvent?.Invoke(i);

                if (quantity <= 0) return 0;
            }
        }
        return quantity;
    }

    // --- ��������� (3) ---
    // ������ ���� ����� 'private', ��� ��� �� ������ ���������� ������ �������
    private void NotifySlotChange(List<InventorySlot> list, int index)
    {
        if (list == mainInventory)
        {
            OnMainInventorySlotUpdated?.Invoke(index);
        }
        else if (list == quickSlots)
        {
            OnQuickSlotUpdated?.Invoke(index);
        }
    }
}