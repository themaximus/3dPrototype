using UnityEngine;
using System;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int mainInventorySize = 20;
    public int quickSlotsSize = 4;

    [Header("Inventory Data")]
    [SerializeField]
    private List<InventorySlot> mainInventory = new List<InventorySlot>();
    [SerializeField]
    private List<InventorySlot> quickSlots = new List<InventorySlot>();

    [Header("Drop Settings")]
    public Transform dropPoint;

    private EquipmentManager equipmentManager;

    // --- СОБЫТИЯ ---
    public event Action<int> OnMainInventorySlotUpdated;
    public event Action<int> OnQuickSlotUpdated;
    public event Action<int, int> OnActiveQuickSlotChanged;

    private int activeQuickSlotIndex = -1;

    public IReadOnlyList<InventorySlot> MainInventory => mainInventory;
    public IReadOnlyList<InventorySlot> QuickSlots => quickSlots;


    void Awake()
    {
        equipmentManager = GetComponent<EquipmentManager>();
        if (equipmentManager == null)
        {
            Debug.LogError("[InventorySystem] Не найден EquipmentManager! Экипировка не будет работать.", this);
        }

        InitializeInventory(mainInventory, mainInventorySize);
        InitializeInventory(quickSlots, quickSlotsSize);

        if (dropPoint == null)
        {
            dropPoint = transform;
        }

        Debug.Log("[System] Запуск InventorySystem...");

        activeQuickSlotIndex = 0;
        EquipItemFromSlot(activeQuickSlotIndex);
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

        // (Логика добавления)
        if (item.isStackable)
        {
            int remainingQuantity = AddToExistingStacks(quickSlots, item, quantity, OnQuickSlotUpdated);
            if (remainingQuantity <= 0) return true;
            remainingQuantity = AddToExistingStacks(mainInventory, item, remainingQuantity, OnMainInventorySlotUpdated);
            if (remainingQuantity <= 0) return true;
            quantity = remainingQuantity;
        }
        int emptySlotIndex = FindEmptySlot(quickSlots);
        if (emptySlotIndex != -1)
        {
            quickSlots[emptySlotIndex] = new InventorySlot(item, quantity);
            OnQuickSlotUpdated?.Invoke(emptySlotIndex);

            if (emptySlotIndex == activeQuickSlotIndex)
            {
                EquipItemFromSlot(activeQuickSlotIndex);
            }
            return true;
        }
        emptySlotIndex = FindEmptySlot(mainInventory);
        if (emptySlotIndex != -1)
        {
            mainInventory[emptySlotIndex] = new InventorySlot(item, quantity);
            OnMainInventorySlotUpdated?.Invoke(emptySlotIndex);
            return true;
        }

        Debug.Log("Нет места в инвентаре! (И в быстрых слотах тоже)");
        return false;
    }

    /// <summary>
    /// Перемещает или "свапает" ВЕСЬ СТАК (вызывается UI Drag & Drop без Shift)
    /// </summary>
    public void MoveItem(bool fromIsQuick, int fromIndex, bool toIsQuick, int toIndex)
    {
        List<InventorySlot> fromList = fromIsQuick ? quickSlots : mainInventory;
        List<InventorySlot> toList = toIsQuick ? quickSlots : mainInventory;
        InventorySlot fromSlot = fromList[fromIndex];
        InventorySlot toSlot = toList[toIndex];

        // --- Логика слияния ---
        if (!fromSlot.IsEmpty() && !toSlot.IsEmpty() &&
            fromSlot.itemData == toSlot.itemData &&
            toSlot.itemData.isStackable &&
            toSlot.quantity < toSlot.itemData.maxStackSize)
        {
            int spaceLeft = toSlot.itemData.maxStackSize - toSlot.quantity;
            int amountToMove = Mathf.Min(fromSlot.quantity, spaceLeft);
            toSlot.AddQuantity(amountToMove);
            fromSlot.RemoveQuantity(amountToMove);
        }
        else
        {
            // --- Логика свапа ---
            fromList[fromIndex] = toSlot;
            toList[toIndex] = fromSlot;
        }

        // 1. Обновляем UI
        NotifySlotChange(fromList, fromIndex);
        NotifySlotChange(toList, toIndex);

        // 2. Проверяем, не затронули ли мы активный слот
        if ((fromIsQuick && fromIndex == activeQuickSlotIndex) || (toIsQuick && toIndex == activeQuickSlotIndex))
        {
            EquipItemFromSlot(activeQuickSlotIndex);
        }
    }

    // --- НОВЫЙ МЕТОД ---
    /// <summary>
    /// Перемещает ОДНУ ЕДИНИЦУ предмета (вызывается UI Drag & Drop + Shift)
    /// </summary>
    public void MoveOneItem(bool fromIsQuick, int fromIndex, bool toIsQuick, int toIndex)
    {
        List<InventorySlot> fromList = fromIsQuick ? quickSlots : mainInventory;
        List<InventorySlot> toList = toIsQuick ? quickSlots : mainInventory;

        InventorySlot fromSlot = fromList[fromIndex];
        InventorySlot toSlot = toList[toIndex];

        // 1. Проверяем, что в 'fromSlot' вообще что-то есть
        if (fromSlot.IsEmpty()) return;

        // 2. Проверяем, что предмет стакается (если нет, то "переместить 1" = "переместить всё")
        if (!fromSlot.itemData.isStackable)
        {
            // Если предмет не стакается, просто вызываем обычный MoveItem
            MoveItem(fromIsQuick, fromIndex, toIsQuick, toIndex);
            return;
        }

        // 3. Логика перемещения одной штуки

        // Случай А: 'toSlot' пустой
        if (toSlot.IsEmpty())
        {
            // Перемещаем 1
            toSlot.itemData = fromSlot.itemData;
            toSlot.AddQuantity(1);
            fromSlot.RemoveQuantity(1);
        }
        // Случай Б: 'toSlot' содержит тот же предмет И в нем есть место
        else if (toSlot.itemData == fromSlot.itemData &&
                 toSlot.quantity < toSlot.itemData.maxStackSize)
        {
            // Перемещаем 1
            toSlot.AddQuantity(1);
            fromSlot.RemoveQuantity(1);
        }
        // Случай В: 'toSlot' занят другим предметом или полон
        else
        {
            // Ничего не делаем. Можно было бы сделать "свап",
            // но "свап одной штуки" - это нелогично.
            // Просто возвращаем.
            return;
        }

        // 4. Обновляем UI
        NotifySlotChange(fromList, fromIndex);
        NotifySlotChange(toList, toIndex);

        // 5. Проверяем, не затронули ли мы активный слот
        if ((fromIsQuick && fromIndex == activeQuickSlotIndex) || (toIsQuick && toIndex == activeQuickSlotIndex))
        {
            EquipItemFromSlot(activeQuickSlotIndex);
        }
    }
    // --- КОНЕЦ НОВОГО МЕТОДА ---

    public void DropItem(bool fromIsQuick, int fromIndex)
    {
        // (Логика выбрасывания)
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

        if (fromIsQuick && fromIndex == activeQuickSlotIndex)
        {
            EquipItemFromSlot(activeQuickSlotIndex);
        }
    }

    public void UseItem(bool isQuickSlot, int slotIndex)
    {
        // (Логика использования)
        List<InventorySlot> list = isQuickSlot ? quickSlots : mainInventory;
        if (slotIndex < 0 || slotIndex >= list.Count) return;
        InventorySlot slot = list[slotIndex];
        if (slot.IsEmpty()) return;
        ItemData data = slot.itemData;

        if (data.itemType != ItemType.Consumable)
        {
            Debug.Log($"[System] Предмет {data.itemName} не является 'Consumable'.");
            return;
        }

        data.Use(this.gameObject);
        slot.RemoveQuantity(1);
        NotifySlotChange(list, slotIndex);

        if (isQuickSlot && slotIndex == activeQuickSlotIndex)
        {
            EquipItemFromSlot(activeQuickSlotIndex);
        }
    }

    // --- БЛОК ЛОГИКИ БЫСТРЫХ СЛОТОВ ---

    public void SetActiveQuickSlot(int index)
    {
        if (index < -1 || index >= quickSlotsSize)
        {
            index = -1;
        }
        if (activeQuickSlotIndex == index)
        {
            // (логика логов)
            return;
        }

        // ЭКИПИРУЕМ ПРЕДМЕТ
        EquipItemFromSlot(index);

        if (activeQuickSlotIndex != index)
        {
            int oldIndex = activeQuickSlotIndex;
            activeQuickSlotIndex = index;
            OnActiveQuickSlotChanged?.Invoke(oldIndex, activeQuickSlotIndex);
            Debug.Log($"[System] Активный UI-слот изменен на: {index}");
        }
    }

    /// <summary>
    /// Внутренний метод, отвечающий за логику экипировки
    /// </summary>
    private void EquipItemFromSlot(int slotIndex)
    {
        if (equipmentManager == null) return;

        if (slotIndex < 0 || slotIndex >= quickSlots.Count)
        {
            equipmentManager.EquipItem(null);
            return;
        }

        InventorySlot slot = quickSlots[slotIndex];

        if (slot.itemData != null)
        {
            equipmentManager.EquipItem(slot.itemData);
        }
        else
        {
            equipmentManager.EquipItem(null);
        }
    }

    public int GetActiveQuickSlotIndex()
    {
        return activeQuickSlotIndex;
    }

    /// <summary>
    /// Вызывается из QuickSlotInputHandler
    /// </summary>
    public void UseActiveQuickSlotItem()
    {
        if (activeQuickSlotIndex < 0)
        {
            Debug.Log("Нет активного слота для использования");
            return;
        }

        // Вызываем уже существующий метод UseItem
        UseItem(true, activeQuickSlotIndex);
    }

    // --- Вспомогательные методы ---

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