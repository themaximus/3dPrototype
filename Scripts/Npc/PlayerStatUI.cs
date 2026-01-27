using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerStatUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private StatController playerStats;

    [Header("Авто-генерация UI")]
    [Tooltip("Префаб текста (TextMeshProUGUI), который будет клонироваться для каждого стата")]
    [SerializeField] private GameObject statTextPrefab;

    [Tooltip("Родительский объект (панель), куда будут добавляться тексты")]
    [SerializeField] private Transform uiContainer;

    // Внутренний класс для хранения связи между Типом и созданным Текстом
    private class RuntimeBinding
    {
        public StatType type;
        public TextMeshProUGUI textComponent;
        public string format;
    }

    private List<RuntimeBinding> activeBindings = new List<RuntimeBinding>();

    void Start()
    {
        // 1. Ищем контроллер, если не назначен
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerStats = player.GetComponent<StatController>();
        }

        if (playerStats == null || playerStats.characterStats == null)
        {
            Debug.LogError("[PlayerStatUI] StatController или CharacterStats не найдены!");
            return;
        }

        // 2. Генерируем UI элементы
        GenerateStatTexts();

        // 3. Подписываемся на изменения
        playerStats.OnStatChanged += UpdateStatUI;

        // 4. Принудительно обновляем значения, чтобы при старте не было пусто
        ForceUpdateAll();
    }

    private void GenerateStatTexts()
    {
        // Очищаем контейнер от старых объектов (если есть)
        foreach (Transform child in uiContainer)
        {
            Destroy(child.gameObject);
        }
        activeBindings.Clear();

        // Проходимся по всем статам в ScriptableObject
        foreach (var statDef in playerStats.characterStats.statsConfig)
        {
            // Создаем новый текст из префаба
            GameObject newTextObj = Instantiate(statTextPrefab, uiContainer);
            newTextObj.name = $"Stat_{statDef.name}"; // Для красоты в иерархии

            // Настраиваем компонент текста
            TextMeshProUGUI tmp = newTextObj.GetComponent<TextMeshProUGUI>();

            // Если нужно, можно покрасить текст в зависимости от типа (опционально)
            // if (statDef.type == StatType.Health) tmp.color = Color.red;

            // Сохраняем связь в список
            RuntimeBinding binding = new RuntimeBinding
            {
                type = statDef.type,
                textComponent = tmp,
                format = statDef.format // Берем формат прямо из настроек стата
            };

            activeBindings.Add(binding);
        }
    }

    private void ForceUpdateAll()
    {
        foreach (var statDef in playerStats.characterStats.statsConfig)
        {
            float current = playerStats.GetStatValue(statDef.type);
            float max = statDef.maxValue;
            UpdateStatUI(statDef.type, current, max);
        }
    }

    private void UpdateStatUI(StatType type, float current, float max)
    {
        // Ищем нужный текст в нашем сгенерированном списке
        foreach (var binding in activeBindings)
        {
            if (binding.type == type)
            {
                binding.textComponent.text = string.Format(binding.format, current, max);
                return;
            }
        }
    }

    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnStatChanged -= UpdateStatUI;
        }
    }
}