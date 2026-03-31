using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class UI_InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI Components")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject highlightVisual;

    // --- Данные слота ---
    private UI_InventoryManager uiManager;
    private InventorySystem inventorySystem;
    private bool isQuickSlot;
    private int slotIndex;

    // --- Компоненты ---
    private CanvasGroup canvasGroup;

    private static bool dropSuccessful;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        ClearVisuals();
    }

    public void Initialize(UI_InventoryManager manager, InventorySystem invSystem, int index, bool isQuick)
    {
        uiManager = manager;
        inventorySystem = invSystem;
        slotIndex = index;
        isQuickSlot = isQuick;

        SetHighlight(false);
        UpdateVisuals();
    }

    private InventorySlot GetSlotData()
    {
        IReadOnlyList<InventorySlot> list = isQuickSlot ? inventorySystem.QuickSlots : inventorySystem.MainInventory;
        if (slotIndex < list.Count)
        {
            return list[slotIndex];
        }
        return null;
    }

    public void UpdateVisuals()
    {
        InventorySlot representedSlot = GetSlotData();

        if (representedSlot == null || representedSlot.IsEmpty())
        {
            ClearVisuals();
            return;
        }

        if (itemIcon.sprite != representedSlot.itemData.itemIcon)
        {
            itemIcon.sprite = representedSlot.itemData.itemIcon;
        }
        itemIcon.enabled = true;

        bool showQuantity = representedSlot.quantity > 1;
        if (showQuantity)
        {
            quantityText.text = "x" + representedSlot.quantity.ToString();
            quantityText.enabled = true;
        }
        else
        {
            quantityText.enabled = false;
        }
    }

    private void ClearVisuals()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        if (quantityText != null)
        {
            quantityText.text = "";
            quantityText.enabled = false;
        }
    }

    // --- Интерфейсы Drag & Drop ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            eventData.pointerDrag = null;
            return;
        }

        InventorySlot representedSlot = GetSlotData();
        if (representedSlot == null || representedSlot.IsEmpty())
        {
            eventData.pointerDrag = null;
            return;
        }

        dropSuccessful = false;
        uiManager.StartDragItem(representedSlot.itemData.itemIcon);
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) return;
        uiManager.UpdateDragItemPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) return;

        uiManager.StopDragItem();
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;

        if (!dropSuccessful)
        {
            if (eventData.pointerEnter == null ||
                eventData.pointerEnter.GetComponent<UI_InventorySlot>() == null)
            {
                inventorySystem.DropItem(this.isQuickSlot, this.slotIndex);
            }
        }
    }

    // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
    public void OnDrop(PointerEventData eventData)
    {
        UI_InventorySlot incomingSlot = eventData.pointerDrag.GetComponent<UI_InventorySlot>();

        if (incomingSlot != null && incomingSlot != this)
        {
            dropSuccessful = true;

            // Проверяем, зажат ли Shift
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                // Если да, вызываем новый метод "Переместить 1"
                inventorySystem.MoveOneItem(
                    incomingSlot.isQuickSlot,
                    incomingSlot.slotIndex,
                    this.isQuickSlot,
                    this.slotIndex
                );
            }
            else
            {
                // Если нет, вызываем старый метод "Переместить всё"
                inventorySystem.MoveItem(
                    incomingSlot.isQuickSlot,
                    incomingSlot.slotIndex,
                    this.isQuickSlot,
                    this.slotIndex
                );
            }
        }
    }
    // --- КОНЕЦ ИЗМЕНЕНИЯ ---


    // --- Метод из Шага 2 (Правый клик) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventorySlot representedSlot = GetSlotData();
            if (representedSlot != null && !representedSlot.IsEmpty())
            {
                Debug.Log($"ПКМ на {representedSlot.itemData.itemName} в слоте {slotIndex}");
                inventorySystem.UseItem(this.isQuickSlot, this.slotIndex);
            }
        }
    }

    // --- Метод из Шага 3 (Подсветка) ---
    public void SetHighlight(bool isActive)
    {
        if (highlightVisual != null)
        {
            highlightVisual.SetActive(isActive);
        }
    }
}