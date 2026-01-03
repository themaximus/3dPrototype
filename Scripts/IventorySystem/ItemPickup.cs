using UnityEngine;

// Префаб предмета в мире ДОЛЖЕН иметь Rigidbody и Collider
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Data")]
    // Ссылка на ScriptableObject предмета
    public ItemData itemData;
    // Количество в этом "стаке"
    public int quantity = 1;

    [Header("Pickup Settings")]
    [Tooltip("Если true, предмет нельзя будет подобрать в инвентарь (только физически таскать)")]
    public bool isNonInventoryItem = false;

    // --- ВАЖНЫЙ БЛОК ---
    // Этот тег должен стоять на префабе,
    // чтобы твой СУЩЕСТВУЮЩИЙ PickupController.cs мог его физически таскать.
    private const string PICKUPABLE_TAG = "Pickupable";

    void Awake()
    {
        // Убедимся, что у префаба правильный тег для твоего PickupController
        if (!gameObject.CompareTag(PICKUPABLE_TAG))
        {
            gameObject.tag = PICKUPABLE_TAG;
            Debug.LogWarning($"На {gameObject.name} не было тега '{PICKUPABLE_TAG}'. Добавляю автоматически. Убедись, что твой PickupController ищет этот тег.");
        }
    }

    /// <summary>
    /// Этот метод вызывается из InventorySystem, когда предмет выбрасывается
    /// </summary>
    public void SetItem(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;

        // Если у предмета нет специальной 3D модели, можно попробовать
        // установить спрайт или что-то еще, но пока это не обязательно
    }
}