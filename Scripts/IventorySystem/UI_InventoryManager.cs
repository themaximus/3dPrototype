using UnityEngine;
using UnityEngine.UI; // <-- ДОБАВЛЕНО
using System.Collections.Generic;

public class UI_InventoryManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Ссылка на 'мозг' инвентаря (обычно на Игроке)")]
    [SerializeField] private InventorySystem inventorySystem;

    [Tooltip("Префаб UI-слота, который мы создали")]
    [SerializeField] private GameObject slotPrefab;

    // --- НОВОЕ ПОЛЕ ---
    [Tooltip("Ссылка на 'плавающий' Image, который следует за мышкой")]
    [SerializeField] private Image dragItemIcon;
    // -------------------

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
        ToggleInventory(false);
    }

    void Start()
    {
        if (inventorySystem == null || slotPrefab == null)
        {
            Debug.LogError("UI_InventoryManager: Не все ссылки настроены в инспекторе!", this);
            return;
        }

        // --- ДОБАВЛЕНО ---
        // Проверяем и прячем 'плавающий' значок при старте
        if (dragItemIcon == null)
        {
            Debug.LogError("UI_InventoryManager: 'Drag Item Icon' не назначен в инспекторе!");
        }
        else
        {
            dragItemIcon.gameObject.SetActive(false);
        }
        // ------------------

        // 1. Создаем UI-слоты
        CreateSlotGrid(inventorySystem.MainInventory, mainInventoryContainer, mainSlotsUI, false);
        CreateSlotGrid(inventorySystem.QuickSlots, quickSlotContainer, quickSlotsUI, true);

        // 2. ПОДПИСЫВАЕМСЯ на события "мозга"
        inventorySystem.OnMainInventorySlotUpdated += UpdateMainSlot;
        inventorySystem.OnQuickSlotUpdated += UpdateQuickSlot;
        inventorySystem.OnActiveQuickSlotChanged += HandleActiveSlotChange;

        // Устанавливаем подсветку на стартовый активный слот
        int initialActiveSlot = inventorySystem.GetActiveQuickSlotIndex();
        if (initialActiveSlot >= 0 && initialActiveSlot < quickSlotsUI.Count)
        {
            quickSlotsUI[initialActiveSlot].SetHighlight(true);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory(inventoryCanvasGroup.alpha == 0);
        }
    }

    // --- НОВЫЕ МЕТОДЫ (3 ШТ) ---

    /// <summary>
    /// Вызывается из UI_InventorySlot, когда начинается перетаскивание
    /// </summary>
    public void StartDragItem(Sprite icon)
    {
        if (dragItemIcon == null) return;

        dragItemIcon.sprite = icon;
        dragItemIcon.gameObject.SetActive(true);
        // Сразу же перемещаем значок к мышке
        UpdateDragItemPosition(Input.mousePosition);
    }

    /// <summary>
    /// Вызывается из UI_InventorySlot, пока мы тащим предмет
    /// </summary>
    public void UpdateDragItemPosition(Vector2 mousePosition)
    {
        if (dragItemIcon == null || !dragItemIcon.gameObject.activeInHierarchy) return;

        dragItemIcon.transform.position = mousePosition;
    }

    /// <summary>
    /// Вызывается из UI_InventorySlot, когда мы отпускаем предмет
    /// </summary>
    public void StopDragItem()
    {
        if (dragItemIcon == null) return;

        dragItemIcon.sprite = null;
        dragItemIcon.gameObject.SetActive(false);
    }
    // ----------------------------


    private void HandleActiveSlotChange(int oldIndex, int newIndex)
    {
        if (oldIndex >= 0 && oldIndex < quickSlotsUI.Count)
        {
            quickSlotsUI[oldIndex].SetHighlight(false);
        }
        if (newIndex >= 0 && newIndex < quickSlotsUI.Count)
        {
            quickSlotsUI[newIndex].SetHighlight(true);
        }
    }

    public void ToggleInventory(bool show)
    {
        if (show)
        {
            inventoryCanvasGroup.alpha = 1;
            inventoryCanvasGroup.interactable = true;
            inventoryCanvasGroup.blocksRaycasts = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            inventoryCanvasGroup.alpha = 0;
            inventoryCanvasGroup.interactable = false;
            inventoryCanvasGroup.blocksRaycasts = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void CreateSlotGrid(IReadOnlyList<InventorySlot> inventoryData, Transform container, List<UI_InventorySlot> uiList, bool isQuickSlot)
    {
        for (int i = 0; i < inventoryData.Count; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, container);
            UI_InventorySlot uiSlot = slotGO.GetComponent<UI_InventorySlot>();

            // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
            // Теперь мы передаем 'this' (ссылку на этот UI_InventoryManager) в слот
            uiSlot.Initialize(this, inventorySystem, i, isQuickSlot);
            // -------------------------

            uiList.Add(uiSlot);
        }
    }

    private void UpdateMainSlot(int index)
    {
        if (index < mainSlotsUI.Count)
        {
            mainSlotsUI[index].UpdateVisuals();
        }
    }

    private void UpdateQuickSlot(int index)
    {
        if (index < quickSlotsUI.Count)
        {
            quickSlotsUI[index].UpdateVisuals();
        }
    }

    void OnDestroy()
    {
        if (inventorySystem != null)
        {
            inventorySystem.OnMainInventorySlotUpdated -= UpdateMainSlot;
            inventorySystem.OnQuickSlotUpdated -= UpdateQuickSlot;
            inventorySystem.OnActiveQuickSlotChanged -= HandleActiveSlotChange;
        }
    }
}