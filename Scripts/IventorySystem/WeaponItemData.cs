using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Item", menuName = "Inventory/Weapon Item")]
public class WeaponItemData : ItemData
{
    [Header("Weapon Settings")]

    [Tooltip("Ссылка на ScriptableObject со статами (урон, дальность...)")]
    // Это ссылка на твой УЖЕ СУЩЕСТВУЮЩИЙ WeaponData.cs
    public WeaponData weaponStats;

    // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
    // Поле 'handModelPrefab' удалено.
    // Оно теперь автоматически наследуется из родительского класса ItemData.
    // -------------------------

    // Этот метод "переопределяет" родительский
    public override void Use(GameObject user)
    {
        // "Использовать" = "Экипировать"

        // 1. Находим EquipmentManager на том, кто нас 'использует' (на Игроке)
        EquipmentManager equipmentManager = user.GetComponent<EquipmentManager>();

        if (equipmentManager != null)
        {
            // 2. Говорим ему экипировать ЭТОТ предмет (this)
            // (Так как 'this' является ItemData, EquipmentManager его примет)
            equipmentManager.EquipItem(this);
        }
        else
        {
            Debug.LogWarning($"Не удалось найти EquipmentManager на {user.name} при использовании {itemName}");
        }
    }

    /// <summary>
    /// Этот метод вызывается автоматически, когда ты создаешь ассет
    /// или когда скрипт загружается.
    /// </summary>
    private void OnValidate()
    {
        // Автоматически устанавливаем правильный тип предмета
        itemType = ItemType.Weapon;
        isStackable = false;
        maxStackSize = 1;
    }
}