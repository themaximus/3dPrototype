using UnityEngine;

public class PickupController : MonoBehaviour
{
    public string pickupTag = "Pickupable"; // Тег объектов для поднятия
    public float pickupDistance = 3f; // Максимальная дистанция для поднятия
    public float holdDistance = 2f; // Расстояние удержания объекта перед камерой
    public float moveSmoothness = 20f; // Скорость плавного перемещения
    public float throwForce = 10f; // Сила броска
    public float maxDistanceToHold = 5f; // Максимальная дистанция для удержания

    private Camera playerCamera;
    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private Collider heldObjectCollider;
    public Collider[] ignoreColliders; // Массив коллайдеров, которые нужно игнорировать

    // ДОБАВЛЕНО: Поле для хранения исходного режима обнаружения столкновений.
    private CollisionDetectionMode originalCollisionMode;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("Камера не найдена! Убедитесь, что в сцене есть Main Camera.");
        }

        // Пример: игнорируем коллайдеры объектов с тегом "Player"
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                // Улучшение: убираем поиск коллайдера на камере, т.к. его там обычно нет
                Collider playerCollider = characterController.GetComponent<Collider>();
                ignoreColliders = new Collider[] { playerCollider };
            }
            else
            {
                Debug.LogWarning("CharacterController не найден у игрока.");
            }
        }
        else
        {
            Debug.LogWarning("Игрок не найден!");
        }
    }

    void Update()
    {
        if (heldObject != null)
        {
            // Отпустить объект
            if (Input.GetKeyUp(KeyCode.E) || IsTooFarFromObject())
            {
                DropObject();
            }

            // Бросить объект
            if (Input.GetMouseButtonDown(0)) // Левая кнопка мыши
            {
                ThrowObject();
            }
        }
        else
        {
            // Проверка возможности поднятия объекта
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryPickupObject();
            }
        }
    }

    void FixedUpdate()
    {
        if (heldObject != null)
        {
            HoldObject();
        }
    }

    bool IsTooFarFromObject()
    {
        // Проверяем расстояние между игроком (камерой) и объектом
        if (heldObject != null)
        {
            float distance = Vector3.Distance(playerCamera.transform.position, heldObject.transform.position);
            return distance > maxDistanceToHold;
        }
        return false;
    }

    void TryPickupObject()
    {
        // Улучшение: используем центр экрана для луча, это надежнее для FPS-игр
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance))
        {
            if (hit.collider.CompareTag(pickupTag))
            {
                PickupObject(hit.collider.gameObject);
            }
        }
    }

    void PickupObject(GameObject obj)
    {
        heldObject = obj;
        heldObjectRb = obj.GetComponent<Rigidbody>();
        heldObjectCollider = obj.GetComponent<Collider>();

        if (heldObjectRb != null)
        {
            // ИЗМЕНЕНО: Сохраняем исходный режим обнаружения столкновений объекта.
            originalCollisionMode = heldObjectRb.collisionDetectionMode;

            heldObjectRb.useGravity = false;
            heldObjectRb.constraints = RigidbodyConstraints.FreezeRotation;

            foreach (var ignoreCollider in ignoreColliders)
            {
                if (ignoreCollider != null && heldObjectCollider != null)
                {
                    Physics.IgnoreCollision(heldObjectCollider, ignoreCollider, true);
                }
            }
        }
    }

    void HoldObject()
    {
        if (heldObjectRb != null)
        {
            Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            Vector3 direction = (targetPosition - heldObjectRb.position);
            heldObjectRb.velocity = direction * moveSmoothness;
        }
    }

    void DropObject()
    {
        if (heldObjectRb != null)
        {
            heldObjectRb.useGravity = true;
            heldObjectRb.constraints = RigidbodyConstraints.None;

            // ИЗМЕНЕНО: Восстанавливаем исходный режим, чтобы вернуть объект в нормальное состояние.
            heldObjectRb.collisionDetectionMode = originalCollisionMode;

            foreach (var ignoreCollider in ignoreColliders)
            {
                if (ignoreCollider != null && heldObjectCollider != null)
                {
                    Physics.IgnoreCollision(heldObjectCollider, ignoreCollider, false);
                }
            }
        }

        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;
    }

    void ThrowObject()
    {
        if (heldObjectRb != null)
        {
            heldObjectRb.useGravity = true;
            heldObjectRb.constraints = RigidbodyConstraints.None;

            // --- ГЛАВНОЕ ИСПРАВЛЕНИЕ ---
            // Устанавливаем режим обнаружения столкновений на 'ContinuousDynamic'.
            // Это заставляет Unity использовать более точный алгоритм для расчета столкновений
            // для быстро движущихся объектов, предотвращая их "прохождение" сквозь другие коллайдеры (туннелирование).
            heldObjectRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            heldObjectRb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);

            foreach (var ignoreCollider in ignoreColliders)
            {
                if (ignoreCollider != null && heldObjectCollider != null)
                {
                    Physics.IgnoreCollision(heldObjectCollider, ignoreCollider, false);
                }
            }
        }

        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;
    }
}
