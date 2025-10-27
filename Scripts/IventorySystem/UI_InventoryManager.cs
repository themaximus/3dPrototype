using UnityEngine;
using System.Collections.Generic;

public class UI_InventoryManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Ссылка на 'мозг' инвентаря (обычно на Игроке)")]
    [SerializeField] private InventorySystem inventorySystem;

    [Tooltip("Префаб UI-слота, который мы создали")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Containers")]
    [Tooltip("Панель с GridLayoutGroup для основного инвентаря")]
    [SerializeField] private Transform mainInventoryContainer;

    [Tooltip("Панель с GridLayoutGroup для быстрых слотов")]
    [SerializeField] private Transform quickSlotContainer;

    [Header("Toggle Settings")]
    [Tooltip("Клавиша для открытия/закрытия инвентаря")]
    public KeyCode toggleKey = KeyCode.I;

    // Списки для хранения ссылок на созданные UI-слоты
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

        // Инвентарь по умолчанию должен быть скрыт
        ToggleInventory(false);
    }

    void Start()
    {
        if (inventorySystem == null || slotPrefab == null)
        {
            Debug.LogError("UI_InventoryManager: Не все ссылки настроены в инспекторе!", this);
            return;
        }

        // --- ГЛАВНАЯ ЛОГИКА ---
        // 1. Создаем UI-слоты
        CreateSlotGrid(inventorySystem.MainInventory, mainInventoryContainer, mainSlotsUI, false);
        CreateSlotGrid(inventorySystem.QuickSlots, quickSlotContainer, quickSlotsUI, true);

        // 2. ПОДПИСЫВАЕМСЯ на события "мозга"
        // Это - сердце архитектуры.
        inventorySystem.OnMainInventorySlotUpdated += UpdateMainSlot;
        inventorySystem.OnQuickSlotUpdated += UpdateQuickSlot;
        // -------------------------
    }

    void Update()
    {
        // Логика открытия/закрытия инвентаря
        if (Input.GetKeyDown(toggleKey))
        {
            // Если инвентарь открыт - закрываем, и наоборот
            ToggleInventory(inventoryCanvasGroup.alpha == 0);
        }
    }

    /// <summary>
    /// Включает или выключает UI инвентаря
    /// </summary>
    public void ToggleInventory(bool show)
    {
        if (show)
        {
            inventoryCanvasGroup.alpha = 1;
            inventoryCanvasGroup.interactable = true;
            inventoryCanvasGroup.blocksRaycasts = true;

            // Включаем курсор, чтобы взаимодействовать с UI
            // (Это как в твоем GameManager)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            inventoryCanvasGroup.alpha = 0;
            inventoryCanvasGroup.interactable = false;
            inventoryCanvasGroup.blocksRaycasts = false;

            // Возвращаем управление игроку
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Автоматически создает сетку UI-слотов
    /// </summary>
    private void CreateSlotGrid(IReadOnlyList<InventorySlot> inventoryData, Transform container, List<UI_InventorySlot> uiList, bool isQuickSlot)
    {
        for (int i = 0; i < inventoryData.Count; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, container);
            UI_InventorySlot uiSlot = slotGO.GetComponent<UI_InventorySlot>();

            // "Заряжаем" UI-слот, давая ему все нужные ссылки
            uiSlot.Initialize(inventorySystem, inventoryData[i], i, isQuickSlot);

            uiList.Add(uiSlot);
        }
    }

    // --- МЕТОДЫ, КОТОРЫЕ ВЫЗЫВАЮТСЯ СОБЫТИЯМИ ---

    /// <summary>
    /// Этот метод вызывается СОБЫТИЕМ OnMainInventorySlotUpdated из InventorySystem
    /// </summary>
    private void UpdateMainSlot(int index)
    {
        if (index < mainSlotsUI.Count)
        {
            mainSlotsUI[index].UpdateVisuals();
        }
    }

    /// <summary>
    /// Этот метод вызывается СОБЫТИЕМ OnQuickSlotUpdated из InventorySystem
    /// </summary>
    private void UpdateQuickSlot(int index)
    {
        if (index < quickSlotsUI.Count)
        {
            quickSlotsUI[index].UpdateVisuals();
        }
    }

    /// <summary>
    /// Важно отписаться от событий, когда объект уничтожается
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