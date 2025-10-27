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

    public override void Use()
    {
        // "Использовать" = "Экипировать"
        Debug.Log("Equipping " + itemName + " to " + slot);

        // Позже здесь будет вызов InventorySystem.Equip(this)
        base.Use();
    }

    private void OnValidate()
    {
        // Автоматически устанавливаем правильный тип предмета
        itemType = ItemType.Equipment;
    }
}