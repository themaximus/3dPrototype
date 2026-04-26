using UnityEngine;

public class RailSwitch : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Сюда перетащи GameObject (1) - ту точку, где начинается ветка")]
    public RailPath targetBranch;

    [Tooltip("Анимация: Часть рычага, которую надо крутить")]
    public Transform handleModel;

    [Header("Углы поворота ручки")]
    public Vector3 closedAngle = new Vector3(0, 0, -45);
    public Vector3 openAngle = new Vector3(0, 0, 45);

    [Header("Input")]
    public float interactionDistance = 3.0f;
    public KeyCode interactKey = KeyCode.E;

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        // Простая проверка взаимодействия (можно заменить на твой Pickup/Interaction скрипт)
        if (Input.GetKeyDown(interactKey))
        {
            CheckPlayerLook();
        }
    }

    void CheckPlayerLook()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // Если игрок смотрит на коллайдер этого рычага
            if (hit.collider.gameObject == this.gameObject || hit.collider.transform.parent == transform)
            {
                ToggleSwitch();
            }
        }
    }

    [ContextMenu("Toggle Switch")]
    public void ToggleSwitch()
    {
        if (targetBranch == null)
        {
            Debug.LogError("RailSwitch: Не назначен Target Branch!");
            return;
        }

        // 1. Меняем логическое состояние
        targetBranch.isSwitchOpen = !targetBranch.isSwitchOpen;
        Debug.Log($"Стрелка {targetBranch.name} переключена. Открыта: {targetBranch.isSwitchOpen}");

        // 2. Обновляем визуал
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (handleModel != null && targetBranch != null)
        {
            // Плавно крутить в Update было бы красивее, но для прототипа сойдет мгновенно
            handleModel.localRotation = Quaternion.Euler(targetBranch.isSwitchOpen ? openAngle : closedAngle);
        }
    }
}