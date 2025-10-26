using UnityEngine;
using UnityEngine.SceneManagement; // <-- ВАЖНО: для перезапуска сцены

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Это поле мы настроим в Unity: перетащим сюда наш Canvas
    [Header("UI")]
    public GameObject restartCanvas;

    private StatController playerStats;

    void Awake()
    {
        // Настраиваем простой Синглтон (чтобы GameManager был один на всю сцену)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Делаем менеджер "вечным"
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- САМАЯ ВАЖНАЯ ЧАСТЬ ---
        // Ищем игрока и его компонент StatController
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<StatController>();
            if (playerStats != null)
            {
                // Подписываемся на событие смерти игрока.
                // Теперь, когда у игрока вызовется OnDeath?.Invoke(),
                // наш метод HandlePlayerDeath() запустится автоматически.
                playerStats.OnDeath += HandlePlayerDeath;
            }
            else
            {
                Debug.LogError("GameManager: На игроке не найден компонент StatController!");
            }
        }
        else
        {
            Debug.LogError("GameManager: Не найден объект с тегом 'Player'!");
        }

        // Убедимся, что экран перезапуска выключен при старте
        if (restartCanvas != null)
        {
            restartCanvas.SetActive(false);
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("Игрок умер. Показываем экран перезапуска.");

        // 1. Показываем UI
        if (restartCanvas != null)
        {
            restartCanvas.SetActive(true);
        }

        // 2. "Замораживаем" игру
        Time.timeScale = 0f;

        // 3. Показываем курсор мыши, чтобы можно было нажать кнопку
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Отписываемся от события, чтобы избежать ошибок при перезапуске
        if (playerStats != null)
        {
            playerStats.OnDeath -= HandlePlayerDeath;
        }
    }

    // Этот метод мы повесим на кнопку "Перезапустить" в Unity
    public void RestartLevel()
    {
        // 1. "Размораживаем" игру
        Time.timeScale = 1f;

        // 2. Снова прячем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. Перезагружаем текущую сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}