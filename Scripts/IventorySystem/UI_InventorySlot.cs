using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class UI_InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Components")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;

    // --- ������ ����� ---
    private InventorySystem inventorySystem;
    private InventorySlot representedSlot;
    private bool isQuickSlot; // <--- ���� ���� ������ ����� �����
    private int slotIndex;

    // --- ���������� ---
    private CanvasGroup canvasGroup;
    private Transform originalParent;

    private static bool dropSuccessful;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        ClearVisuals();
    }

    public void Initialize(InventorySystem invSystem, InventorySlot slotData, int index, bool isQuick)
    {
        inventorySystem = invSystem;
        representedSlot = slotData;
        slotIndex = index;
        isQuickSlot = isQuick; // <--- ��������� ����

        originalParent = transform.parent;

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
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

    // --- ���������� Drag & Drop ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (representedSlot.IsEmpty())
        {
            eventData.pointerDrag = null;
            return;
        }

        dropSuccessful = false;

        transform.SetParent(GetComponentInParent<Canvas>().transform);

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;

        transform.SetParent(originalParent);
        transform.SetAsLastSibling();

        if (!dropSuccessful)
        {
            if (eventData.pointerEnter == null || eventData.pointerEnter.CompareTag("DropZone"))
            {
                // --- ��������� ����� ---
                // �������� ����� ������ DropItem
                inventorySystem.DropItem(this.isQuickSlot, this.slotIndex);
            }
        }

        UpdateVisuals();
    }

    public void OnDrop(PointerEventData eventData)
    {
        UI_InventorySlot incomingSlot = eventData.pointerDrag.GetComponent<UI_InventorySlot>();

        if (incomingSlot != null && incomingSlot != this)
        {
            dropSuccessful = true;

            // --- ��������� ����� ---
            // �������� ����� ������ MoveItem, ��������� �����
            inventorySystem.MoveItem(
                incomingSlot.isQuickSlot,
                incomingSlot.slotIndex,
                this.isQuickSlot,
                this.slotIndex
            );

            incomingSlot.UpdateVisuals();
        }
    }
}