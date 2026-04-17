using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Ссылка на главную камеру игрока (FPS камеру)")]
    public Camera playerCamera;

    [Header("Взаимодействие")]
    [Tooltip("Как далеко игрок может взаимодействовать с предметами")]
    public float interactDistance = 3f;

    [Tooltip("Клавиша основного действия (Инвентарь, Диалог, Чтение)")]
    public KeyCode interactKey = KeyCode.F;

    [Tooltip("Клавиша вторичного действия (Взять физически в руки)")]
    public KeyCode grabKey = KeyCode.G;

    // Ссылка на любой интерактивный объект в прицеле
    private IInteractable currentTarget;

    void Start()
    {
        // Ищем камеру, если она не привязана
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("PlayerInteractor: Камера не найдена!", this);
                this.enabled = false;
            }
        }
    }

    void Update()
    {
        // 1. Постоянно проверяем, на что смотрим
        HandleHoverLogic();

        // 2. Проверяем нажатия клавиш
        if (currentTarget != null)
        {
            // Основное действие (В инвентарь)
            if (Input.GetKeyDown(interactKey))
            {
                currentTarget.Interact(this.gameObject);
            }

            // Вторичное действие (Взять в руки)
            if (Input.GetKeyDown(grabKey))
            {
                currentTarget.SecondaryInteract(this.gameObject);
            }
        }
    }

    private void HandleHoverLogic()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // Пытаемся найти интерфейс IInteractable на объекте
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != currentTarget)
            {
                // Выключаем старый объект
                if (currentTarget != null) currentTarget.OnHoverExit();

                // Включаем новый
                currentTarget = interactable;
                if (currentTarget != null) currentTarget.OnHoverEnter();
            }
        }
        else
        {
            // Если смотрим в пустоту - сбрасываем цель
            if (currentTarget != null)
            {
                currentTarget.OnHoverExit();
                currentTarget = null;
            }
        }
    }
}