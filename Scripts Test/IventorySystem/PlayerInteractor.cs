using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Ссылка на главную камеру игрока (FPS камеру)")]
    public Camera playerCamera;

    [Header("Interaction Settings")]
    [Tooltip("Как далеко игрок может взаимодействовать")]
    public float interactDistance = 3f;

    [Tooltip("Клавиша для взаимодействия (подбор/диалог)")]
    public KeyCode interactKey = KeyCode.F;

    // Храним ссылку на интерфейс ЛЮБОГО объекта (лут, NPC, дверь)
    private IInteractable currentTarget;

    void Start()
    {
        // Твоя шикарная резервная проверка камеры
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
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
        // 1. Постоянно проверяем, на что смотрим (Hover)
        HandleHoverLogic();

        // 2. Проверяем нажатие клавиши
        if (Input.GetKeyDown(interactKey))
        {
            if (currentTarget != null)
            {
                // Передаем сам объект игрока, чтобы предмет (если это лут) мог найти инвентарь
                currentTarget.Interact(this.gameObject);
            }
        }
    }

    private void HandleHoverLogic()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // Ищем ЛЮБОЙ интерактивный объект
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != currentTarget)
            {
                // Выключаем старый
                if (currentTarget != null) currentTarget.OnHoverExit();

                // Включаем новый
                currentTarget = interactable;
                if (currentTarget != null) currentTarget.OnHoverEnter();
            }
        }
        else
        {
            // Если смотрим в пустоту - сбрасываем
            if (currentTarget != null)
            {
                currentTarget.OnHoverExit();
                currentTarget = null;
            }
        }
    }
}