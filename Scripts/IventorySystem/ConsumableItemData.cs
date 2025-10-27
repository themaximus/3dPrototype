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
    public override void Use()
    {
        // "Использовать" = "Потребить"
        Debug.Log("Consuming " + itemName + ". Restoring " + healthToRestore + " health.");

        // Позже здесь будет логика поиска StatController игрока
        // и вызов Player.GetComponent<StatController>().Heal(healthToRestore);

        // base.Use(); // Вызывать родительский метод не обязательно,
        // так как мы полностью заменяем его логику
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