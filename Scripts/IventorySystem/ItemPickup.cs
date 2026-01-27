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

    private const string PICKUPABLE_TAG = "Pickupable";
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (!gameObject.CompareTag(PICKUPABLE_TAG))
        {
            gameObject.tag = PICKUPABLE_TAG;
        }

        // Применяем вес сразу при старте
        ApplyWeight();
    }

    // Вызывать этот метод, если данные предмета поменялись (например, при спавне)
    public void SetItem(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
        ApplyWeight();
    }

    // --- НОВЫЙ МЕТОД ---
    private void ApplyWeight()
    {
        if (itemData != null && rb != null)
        {
            // Ставим массу Rigidbody равной весу из ScriptableObject
            rb.mass = itemData.weight;
        }
    }
}