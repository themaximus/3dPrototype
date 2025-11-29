using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // <-- ВАЖНО: Добавляем для работы с Button

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject restartCanvas;

    private StatController playerStats;

    void Awake()
    {
        // Настраиваем простой Синглтон
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Подписываемся на событие "сцена загружена".
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Этот метод будет вызываться автоматически каждый раз при загрузке сцены
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("GameManager: Сцена загружена. Ищу нового игрока и UI...");

        // 1. Пытаемся отписаться от старого игрока (если он был)
        if (playerStats != null)
        {
            playerStats.OnDeath -= HandlePlayerDeath;
        }

        // 2. Ищем нового игрока
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<StatController>();
            if (playerStats != null)
            {
                playerStats.OnDeath += HandlePlayerDeath;
                Debug.Log("GameManager: Успешно подписался на смерть нового игрока.");
            }
            else
            {
                Debug.LogError("GameManager: На игроке не найден компонент StatController!");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: Не найден объект с тегом 'Player' на этой сцене.");
        }

        // 3. Ищем НОВЫЙ экран перезапуска на загруженной сцене
        GameObject restartObject = GameObject.FindWithTag("RestartScreen");
        if (restartObject != null)
        {
            // 4. Пере-назначаем нашу переменную на новый объект
            restartCanvas = restartObject;
            Debug.Log("GameManager: Экран перезапуска найден.");

            // --- ИСПРАВЛЕНИЕ ЗДЕСЬ ---
            // 5. Ищем КОМПОНЕНТ КНОПКИ внутри экрана перезапуска
            // (true) в GetComponentInChildren означает "искать даже если объект выключен"
            Button restartButton = restartCanvas.GetComponentInChildren<Button>(true);

            if (restartButton != null)
            {
                // 6. Очищаем старые (сломанные) "слушатели" и добавляем новый
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartLevel); // <-- Привязываем НАШ метод
                Debug.Log("GameManager: Кнопка 'Рестарт' успешно настроена.");
            }
            else
            {
                Debug.LogError("GameManager: НЕ НАЙДЕНА КНОПКА (Button) внутри 'RestartScreen'!");
            }
            // -------------------------

            // 7. Убедимся, что он выключен при старте
            restartCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError("GameManager: НЕ НАЙДЕН 'restartCanvas'! Убедись, что объект с тегом 'RestartScreen' есть на сцене.");
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
        else
        {
            Debug.LogError("GameManager: 'restartCanvas' - null! Не могу показать экран смерти.");
        }

        // 2. "Замораживаем" игру
        Time.timeScale = 0f;

        // 3. Показываем курсор мыши
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Отписываемся от события
        if (playerStats != null)
        {
            playerStats.OnDeath -= HandlePlayerDeath;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}