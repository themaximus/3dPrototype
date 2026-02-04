using UnityEngine;
using TMPro;

public class WorldUIManager : MonoBehaviour
{
    public static WorldUIManager Instance;

    [Header("UI References")]
    [Tooltip("Корневой объект Canvas (должен быть World Space)")]
    public GameObject uiContainer;
    [Tooltip("RectTransform рамки (которая содержит уголки)")]
    public RectTransform frameRect;
    [Tooltip("Текст названия")]
    public TextMeshProUGUI nameText;
    [Tooltip("Текст описания")]
    public TextMeshProUGUI descriptionText;

    [Header("Settings")]
    public float uiScale = 0.005f; // Масштаб канваса, чтобы он был адекватного размера в мире
    public float padding = 0.2f;   // Отступ рамки от краев объекта

    private ItemPickup currentTarget;
    private Transform mainCameraTransform;
    private Renderer targetRenderer;

    void Awake()
    {
        Instance = this;
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        Hide(); // Скрываем при старте
    }

    public void Show(ItemPickup item)
    {
        currentTarget = item;
        targetRenderer = item.GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            // Если у предмета нет Renderer, ищем в детях
            targetRenderer = item.GetComponentInChildren<Renderer>();
        }

        // Заполняем тексты
        nameText.text = item.itemData.itemName;
        descriptionText.text = item.GetDescription();

        uiContainer.SetActive(true);
    }

    public void Hide()
    {
        currentTarget = null;
        targetRenderer = null;
        uiContainer.SetActive(false);
    }

    void LateUpdate()
    {
        if (currentTarget == null || targetRenderer == null || uiContainer.activeSelf == false)
        {
            if (uiContainer.activeSelf) Hide();
            return;
        }

        // 1. Позиционирование: ставим UI в центр объекта
        transform.position = targetRenderer.bounds.center;

        // 2. Вращение: всегда смотрим на камеру (Billboard)
        // Поворачиваем UI так, чтобы он был параллелен камере
        transform.rotation = mainCameraTransform.rotation;

        // 3. Размер рамки: подгоняем под размер объекта
        // bounds.extents - это половина размера (радиус)
        // Берем максимальные измерения, чтобы рамка не сплющивалась
        Vector3 size = targetRenderer.bounds.size;

        // Определяем, насколько большим объект выглядит "в фас"
        float maxDimension = Mathf.Max(size.x, size.y, size.z);

        // Переводим размер мира в размер UI (учитывая масштаб Canvas и Padding)
        float frameSize = (maxDimension + padding) / uiScale;

        // Применяем размер к рамке
        frameRect.sizeDelta = new Vector2(frameSize, frameSize);
    }
}