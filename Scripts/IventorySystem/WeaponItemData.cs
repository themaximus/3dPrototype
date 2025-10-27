using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Item", menuName = "Inventory/Weapon Item")]
public class WeaponItemData : ItemData
{
    [Header("Weapon Settings")]

    [Tooltip("Ссылка на ScriptableObject со статами (урон, дальность...)")]
    // Это ссылка на твой УЖЕ СУЩЕСТВУЮЩИЙ WeaponData.cs
    public WeaponData weaponStats;

    [Tooltip("Префаб, который будет появляться в руках у игрока при экипировке")]
    // У этого префаба должен быть твой HandController/WeaponController
    public GameObject handModelPrefab;

    // Этот метод "переопределяет" родительский
    public override void Use()
    {
        // "Использовать" для оружия в инвентаре - значит "Экипировать"
        // (Позже мы повесим сюда логику)
        Debug.Log("Equipping " + itemName);

        // base.Use(); // Можно вызвать родительский метод, если нужно
    }

    /// <summary>
    /// Этот метод вызывается автоматически, когда ты создаешь ассет
    /// или когда скрипт загружается.
    /// </summary>
    private void OnValidate()
    {
        // Автоматически устанавливаем правильный тип предмета
        itemType = ItemType.Weapon;
    }
}