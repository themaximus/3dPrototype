using UnityEngine;

/// <summary>
/// Простой класс, НЕ MonoBehaviour, который представляет
/// один слот в инвентаре.
/// </summary>
[System.Serializable] // Это позволяет Unity сохранять и отображать этот класс в инспекторе
public class InventorySlot
{
    // Ссылка на шаблон предмета (ScriptableObject)
    public ItemData itemData;
    // Текущее количество предметов в этом слоте
    public int quantity;

    /// <summary>
    /// Конструктор для создания пустого слота
    /// </summary>
    public InventorySlot()
    {
        itemData = null;
        quantity = 0;
    }

    /// <summary>
    /// Конструктор для создания слота с предметом
    /// </summary>
    public InventorySlot(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
    }

    /// <summary>
    /// Помощник: проверяет, пуст ли слот
    /// </summary>
    public bool IsEmpty()
    {
        return itemData == null || quantity <= 0;
    }

    /// <summary>
    /// Очищает слот
    /// </summary>
    public void ClearSlot()
    {
        itemData = null;
        quantity = 0;
    }

    /// <summary>
    /// Добавляет определенное количество к этому слоту
    /// </summary>
    public void AddQuantity(int amount)
    {
        quantity += amount;
    }

    /// <summary>
    /// Убирает определенное количество из этого слота
    /// </summary>
    public void RemoveQuantity(int amount)
    {
        quantity -= amount;
        if (quantity <= 0)
        {
            ClearSlot();
        }
    }
}