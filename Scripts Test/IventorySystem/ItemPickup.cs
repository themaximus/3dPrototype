using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Данные предмета")]
    public ItemData itemData;
    public int quantity = 1;

    [Header("Настройки")]
    public bool isNonInventoryItem = false;
    public bool useWorldUI = true;

    [TextArea(3, 5)]
    public string customDescription;

    private Rigidbody rb;
    private const string PICKUPABLE_TAG = "Pickupable";

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (!gameObject.CompareTag(PICKUPABLE_TAG))
        {
            gameObject.tag = PICKUPABLE_TAG;
        }

        ApplyWeight();
    }

    public void SetItem(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
        ApplyWeight();
    }

    private void ApplyWeight()
    {
        if (itemData != null && rb != null)
        {
            rb.mass = itemData.weight;
        }
    }

    // --- ОСНОВНОЕ ДЕЙСТВИЕ [F] - ПОДОБРАТЬ В ИНВЕНТАРЬ ---
    public void Interact(GameObject player)
    {
        if (isNonInventoryItem) return;

        InventorySystem inventory = player.GetComponent<InventorySystem>();

        if (inventory != null)
        {
            bool successfullyAdded = inventory.AddItem(itemData, quantity);

            if (successfullyAdded)
            {
                // Если предмет лежал у нас в руках, нужно заставить руки его отпустить
                HandController hand = player.GetComponent<HandController>();
                if (hand != null && hand.IsHoldingThis(gameObject))
                {
                    hand.DropObject();
                }

                OnHoverExit(); // Выключаем UI
                Destroy(gameObject); // Уничтожаем предмет со сцены
            }
            else
            {
                Debug.Log("Инвентарь полон!");
            }
        }
    }

    // --- ВТОРИЧНОЕ ДЕЙСТВИЕ [G] - ВЗЯТЬ/ОТПУСТИТЬ В РУКИ ---
    public void SecondaryInteract(GameObject player)
    {
        HandController hand = player.GetComponent<HandController>();
        if (hand != null)
        {
            // Если руки пустые — берем предмет
            if (!hand.IsHolding)
            {
                hand.PickUpObject(this.gameObject);
            }
            // Если мы УЖЕ держим этот предмет и снова нажали G — бросаем его
            else if (hand.IsHoldingThis(gameObject))
            {
                hand.DropObject();
            }
        }
    }

    public void OnHoverEnter()
    {
        if (!useWorldUI) return;
        if (WorldUIManager.Instance != null)
        {
            WorldUIManager.Instance.Show(this);
        }
    }

    public void OnHoverExit()
    {
        if (WorldUIManager.Instance != null)
        {
            WorldUIManager.Instance.Hide();
        }
    }
}