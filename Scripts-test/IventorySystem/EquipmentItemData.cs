using UnityEngine;

/// <summary>
/// Определяет, в какой слот экипировки помещается предмет
/// </summary>
public enum EquipmentSlot
{
    Head,
    Chest,
    Legs,
    Feet,
    // Можешь добавить что угодно: Hands, Shoulders, Amulet...
}

[CreateAssetMenu(fileName = "New Equipment Item", menuName = "Inventory/Equipment Item")]
public class EquipmentItemData : ItemData
{
    [Header("Equipment Settings")]
    public EquipmentSlot slot;

    [Tooltip("На сколько этот предмет снижает входящий урон (в % или ед.)")]
    public int defenseModifier;

    // Сюда можно добавить и другие статы, например, 
    // public float speedModifier;

    // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
    public override void Use(GameObject user)
    {
        // "Использовать" = "Экипировать"
        Debug.Log("Equipping " + itemName + " to " + slot + " on " + user.name);

        // Позже здесь будет вызов InventorySystem.Equip(this)
        // base.Use(user); // Вызываем родительский, если нужно
    }

    private void OnValidate()
    {
        // Автоматически устанавливаем правильный тип предмета
        itemType = ItemType.Equipment;

        // --- ДОБАВЛЕНО ---
        // Экипировка обычно не стакается
        isStackable = false;
        maxStackSize = 1;
        // -----------------
    }
}