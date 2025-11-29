using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Ссылка на главную камеру игрока (FPS камеру)")]
    public Camera playerCamera;

    [Tooltip("Ссылка на 'мозг' инвентаря. Должна найтись автоматически.")]
    private InventorySystem inventorySystem;

    [Header("Interaction Settings")]
    [Tooltip("Как далеко игрок может подбирать предметы")]
    public float pickupDistance = 3f;

    [Tooltip("Клавиша для подбора предмета в инвентарь (E - уже занята физическим подбором)")]
    public KeyCode pickupKey = KeyCode.F;

    void Awake()
    {
        // Автоматически находим InventorySystem на этом же объекте
        inventorySystem = GetComponent<InventorySystem>();
    }

    void Start()
    {
        // Проверка, назначена ли камера
        if (playerCamera == null)
        {
            // Попробуем найти ее автоматически
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                // Если у тебя нет Main Camera, используем камеру из PlayerController
                // (основываясь на твоем PlayerController.cs)
                FirstPersonController controller = GetComponent<FirstPersonController>();
                if (controller != null && controller.cameraTransform != null)
                {
                    playerCamera = controller.cameraTransform.GetComponent<Camera>();
                }
            }

            if (playerCamera == null)
            {
                Debug.LogError("PlayerInteractor: Камера не найдена! Назначь 'playerCamera' вручную в инспекторе.", this);
                this.enabled = false; // Выключаем скрипт, чтобы не было ошибок
            }
        }
    }

    void Update()
    {
        // Проверяем нажатие клавиши подбора
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupItem();
        }
    }

    /// <summary>
    /// Пускает луч из центра экрана и пытается подобрать предмет
    /// </summary>
    private void TryPickupItem()
    {
        // Создаем луч из центра экрана
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        // Пускаем луч на дистанцию 'pickupDistance'
        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            // Проверяем, есть ли на объекте компонент ItemPickup
            ItemPickup itemPickup = hit.collider.GetComponent<ItemPickup>();

            if (itemPickup != null)
            {
                // Проверяем, можно ли этот предмет вообще класть в инвентарь
                if (itemPickup.isNonInventoryItem)
                {
                    // (Сюда можно добавить подсказку "Этот предмет нельзя взять")
                    return;
                }

                // Пытаемся добавить предмет в "мозг" инвентаря
                bool successfullyAdded = inventorySystem.AddItem(itemPickup.itemData, itemPickup.quantity);

                if (successfullyAdded)
                {
                    // Если место нашлось, и предмет добавлен - уничтожаем объект в мире
                    Destroy(hit.collider.gameObject);
                }
                else
                {
                    // (Сюда можно добавить подсказку "Инвентарь полон")
                    Debug.Log("Не удалось поднять " + itemPickup.itemData.itemName + ". Инвентарь полон!");
                }
            }
        }
    }
}