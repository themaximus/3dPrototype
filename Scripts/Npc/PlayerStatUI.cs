using UnityEngine;
using UnityEngine.UI; // <-- ВАЖНО: для работы с Slider
using TMPro; // (Если захочешь добавить текст "100 / 100")

public class PlayerStatUI : MonoBehaviour
{
    [Header("Health Bar")]
    [Tooltip("Ссылка на UI Slider, который показывает ХП")]
    [SerializeField] private Slider healthSlider;

    // (Сюда можно будет добавить Slider для выносливости и т.д.)
    // [SerializeField] private Slider staminaSlider; 

    [Header("Links")]
    [Tooltip("Ссылка на StatController игрока")]
    [SerializeField] private StatController playerStats;

    // (Сюда можно добавить TextMeshProUGUI для текста ХП)
    // [SerializeField] private TextMeshProUGUI healthText;

    void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStatUI: Ссылка на 'playerStats' не установлена в инспекторе!", this);
            this.enabled = false;
            return;
        }

        if (healthSlider == null)
        {
            Debug.LogError("PlayerStatUI: Ссылка на 'healthSlider' не установлена в инспекторе!", this);
        }

        // 1. ПОДПИСЫВАЕМСЯ на событие
        playerStats.OnHealthChanged += UpdateHealthBar;

        // 2. Устанавливаем начальные значения
        // (Мы берем их напрямую на случай, если наш Start() сработает раньше,
        // чем Start() в StatController)
        UpdateHealthBar(playerStats.CurrentHealth, playerStats.characterStats.maxHealth);
    }

    /// <summary>
    /// Этот метод вызывается событием OnHealthChanged из StatController
    /// </summary>
    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // (Если добавишь текст, раскомментируй это)
        // if (healthText != null)
        // {
        //     healthText.text = $"{currentHealth} / {maxHealth}";
        // }
    }

    // Важно отписаться, чтобы не было утечек памяти
    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthBar;
        }
    }
}