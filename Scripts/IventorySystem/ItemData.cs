using UnityEngine;

/// <summary>
/// Определяем базовые типы предметов.
/// </summary>
public enum ItemType
{
    Default,
    Weapon,
    Consumable,
    Equipment
}

[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string itemName;
    [TextArea(3, 5)] // Сделаем поле описания побольше в инспекторе
    public string description;
    public Sprite itemIcon;
    public ItemType itemType = ItemType.Default;

    [Header("World")]
    // Префаб, который будет спавниться при выбрасывании
    // У этого префаба должен быть Rigidbody и Collider
    public GameObject worldPrefab;

    // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
    [Header("Equipping")]
    [Tooltip("Префаб, который будет появляться в руках у игрока при экипировке (если этот предмет можно держать)")]
    public GameObject handModelPrefab;
    // -------------------------

    [Header("Stacking")]
    public bool isStackable = false;
    // maxStackSize будет игнорироваться, если isStackable = false
    public int maxStackSize = 1;

    /// <summary>
    /// Виртуальный метод для "использования" предмета (например, по нажатию ПКМ в инвентаре).
    /// </summary>
    public virtual void Use(GameObject user)
    {
        // По умолчанию - ничего не делает
        Debug.Log("Using: " + itemName + " by " + user.name);
    }
}