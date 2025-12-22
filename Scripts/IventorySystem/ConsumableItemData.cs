using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable Item", menuName = "Inventory/Consumable Item")]
public class ConsumableItemData : ItemData
{
    [Header("Consumable Settings")]
    [Tooltip("Сколько здоровья восстанавливает этот предмет")]
    public int healthToRestore = 0;

    [Tooltip("Сколько голода утоляет этот предмет (если у тебя будет система голода)")]
    public int hungerToRestore = 0;

    // Сюда можно добавить любые другие эффекты:
    // public float speedBoostDuration = 0f;
    // public int manaToRestore = 0;

    /// <summary>
    /// Переопределяем базовый метод Use()
    /// </summary>
    // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
    // Меняем сигнатуру, чтобы она совпадала с родительской (ItemData)
    public override void Use(GameObject user)
    {
        // "Использовать" = "Потребить"
        Debug.Log("Consuming " + itemName + ". Restoring " + healthToRestore + " health.");

        // --- НОВАЯ ЛОГИКА ---
        // Ищем StatController на том, кто использовал предмет (на user)
        StatController userStats = user.GetComponent<StatController>();
        if (userStats != null)
        {
            // Если нашли - вызываем лечение
            userStats.Heal(healthToRestore);
        }
        else
        {
            Debug.LogWarning($"Не удалось найти StatController на {user.name} при использовании {itemName}");
        }
        // ----------------------

        // base.Use(); // Старый вызов больше не нужен
    }

    private void OnValidate()
    {
        // Автоматически устанавливаем правильный тип предмета
        itemType = ItemType.Consumable;
        // Расходуемые предметы почти всегда стакаются
        isStackable = true;
        if (maxStackSize <= 1)
        {
            maxStackSize = 10; // Установим значение по умолчанию
        }
    }
}