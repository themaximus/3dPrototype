using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Ссылка на главную камеру игрока (FPS камеру)")]
    public Camera playerCamera;

    private InventorySystem inventorySystem;

    [Header("Interaction Settings")]
    [Tooltip("Как далеко игрок может подбирать предметы")]
    public float pickupDistance = 3f;

    [Tooltip("Клавиша для подбора предмета")]
    public KeyCode pickupKey = KeyCode.F;

    // Храним ссылку на предмет, на который сейчас смотрим
    private ItemPickup currentHoverItem;

    void Awake()
    {
        inventorySystem = GetComponent<InventorySystem>();
    }

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                // Пытаемся найти через FirstPersonController, как было у вас
                FirstPersonController controller = GetComponent<FirstPersonController>();
                if (controller != null && controller.cameraTransform != null)
                    playerCamera = controller.cameraTransform.GetComponent<Camera>();
            }

            if (playerCamera == null)
            {
                Debug.LogError("PlayerInteractor: Камера не найдена!", this);
                this.enabled = false;
            }
        }
    }

    void Update()
    {
        // 1. Постоянно проверяем, на что смотрим (для подсветки)
        HandleHoverLogic();

        // 2. Проверяем нажатие клавиши подбора
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupItem();
        }
    }

    private void HandleHoverLogic()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            ItemPickup item = hit.collider.GetComponent<ItemPickup>();

            // Если смотрим на новый предмет
            if (item != currentHoverItem)
            {
                // Выключаем старый
                if (currentHoverItem != null) currentHoverItem.OnHoverExit();

                // Включаем новый
                currentHoverItem = item;
                if (currentHoverItem != null) currentHoverItem.OnHoverEnter();
            }
        }
        else
        {
            // Если смотрим в пустоту, но предмет был выбран - сбрасываем
            if (currentHoverItem != null)
            {
                currentHoverItem.OnHoverExit();
                currentHoverItem = null;
            }
        }
    }

    private void TryPickupItem()
    {
        // Используем тот же луч, но теперь для действия
        if (currentHoverItem != null)
        {
            if (currentHoverItem.isNonInventoryItem) return;

            bool successfullyAdded = inventorySystem.AddItem(currentHoverItem.itemData, currentHoverItem.quantity);

            if (successfullyAdded)
            {
                // --- ИСПРАВЛЕНИЕ ОШИБКИ ---

                // 1. Сначала скрываем UI
                currentHoverItem.OnHoverExit();

                // 2. Сохраняем ссылку на GameObject во временную переменную,
                // потому что currentHoverItem мы сейчас обнулим
                GameObject objectToDestroy = currentHoverItem.gameObject;

                // 3. Обнуляем текущий предмет в скрипте, чтобы логика Hover не сломалась
                currentHoverItem = null;

                // 4. Уничтожаем объект
                Destroy(objectToDestroy);
            }
            else
            {
                Debug.Log("Инвентарь полон!");
            }
        }
    }
}