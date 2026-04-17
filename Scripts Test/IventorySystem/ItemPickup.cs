using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData itemData;
    public int quantity = 1;

    [Header("Pickup Settings")]
    public bool isNonInventoryItem = false;

    [Header("Visual Settings")]
    [Tooltip("Показывать ли 3D интерфейс при наведении?")]
    public bool useWorldUI = true;

    [TextArea(3, 5)]
    [Tooltip("Оставьте пустым, чтобы использовать описание из ScriptableObject (рекомендуется)")]
    public string customDescription; // Оставляем поле на случай уникальных записок на уровне

    private const string PICKUPABLE_TAG = "Pickupable";
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (!gameObject.CompareTag(PICKUPABLE_TAG))
        {
            gameObject.tag = PICKUPABLE_TAG;
        }

        ApplyWeight();
    }

    public void SetItem(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
        ApplyWeight();
    }

    private void ApplyWeight()
    {
        if (itemData != null && rb != null)
        {
            rb.mass = itemData.weight;
        }
    }

    // --- ЛОГИКА ОПИСАНИЯ ---

    public string GetDescription()
    {
        // 1. ГЛАВНЫЙ ПРИОРИТЕТ: Данные из ScriptableObject.
        // Так как itemData - это ссылка на файл, описание в нем никогда не сотрется при выбрасывании.
        if (itemData != null && !string.IsNullOrEmpty(itemData.description))
        {
            return itemData.description;
        }

        // 2. ЗАПАСНОЙ ВАРИАНТ: Если в ScriptableObject пусто, берем локальное описание.
        // (Полезно для уникальных объектов на уровне, которые нельзя подобрать)
        if (!string.IsNullOrEmpty(customDescription))
        {
            return customDescription;
        }

        return "Нет описания";
    }

    public void OnHoverEnter()
    {
        if (!useWorldUI) return;
        if (WorldUIManager.Instance != null)
        {
            WorldUIManager.Instance.Show(this);
        }
    }

    public void OnHoverExit()
    {
        if (WorldUIManager.Instance != null)
        {
            WorldUIManager.Instance.Hide();
        }
    }
}