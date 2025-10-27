using UnityEngine;
using UnityEngine.SceneManagement; // <-- �����: ��� ����������� �����

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ��� ���� �� �������� � Unity: ��������� ���� ��� Canvas
    [Header("UI")]
    public GameObject restartCanvas;

    private StatController playerStats;

    void Awake()
    {
        // ����������� ������� �������� (����� GameManager ��� ���� �� ��� �����)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ������ �������� "������"
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- ����� ������ ����� ---
        // ���� ������ � ��� ��������� StatController
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<StatController>();
            if (playerStats != null)
            {
                // ������������� �� ������� ������ ������.
                // ������, ����� � ������ ��������� OnDeath?.Invoke(),
                // ��� ����� HandlePlayerDeath() ���������� �������������.
                playerStats.OnDeath += HandlePlayerDeath;
            }
            else
            {
                Debug.LogError("GameManager: �� ������ �� ������ ��������� StatController!");
            }
        }
        else
        {
            Debug.LogError("GameManager: �� ������ ������ � ����� 'Player'!");
        }

        // ��������, ��� ����� ����������� �������� ��� ������
        if (restartCanvas != null)
        {
            restartCanvas.SetActive(false);
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("����� ����. ���������� ����� �����������.");

        // 1. ���������� UI
        if (restartCanvas != null)
        {
            restartCanvas.SetActive(true);
        }

        // 2. "������������" ����
        Time.timeScale = 0f;

        // 3. ���������� ������ ����, ����� ����� ���� ������ ������
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. ������������ �� �������, ����� �������� ������ ��� �����������
        if (playerStats != null)
        {
            playerStats.OnDeath -= HandlePlayerDeath;
        }
    }

    // ���� ����� �� ������� �� ������ "�������������" � Unity
    public void RestartLevel()
    {
        // 1. "�������������" ����
        Time.timeScale = 1f;

        // 2. ����� ������ ������
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. ������������� ������� �����
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}