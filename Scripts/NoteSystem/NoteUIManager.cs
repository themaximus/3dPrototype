using UnityEngine;
using TMPro;

public class NoteUIManager : MonoBehaviour
{
    public static NoteUIManager Instance;

    [Header("UI Elements")]
    public GameObject notePanel;
    public TextMeshProUGUI noteTextArea;

    [Header("Settings")]
    public bool isNoteOpen = false;

    // --- Ќќ¬јя ѕ≈–≈ћ≈ЌЌјя ---
    private float openTimeStamp = 0f; // ¬рем€, когда записка была открыта
    // ------------------------

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        CloseNote();
    }

    void Update()
    {
        if (!isNoteOpen) return;

        // --- »—ѕ–ј¬Ћ≈Ќ»≈: ЅЋќ »–ќ¬ ј ћ√Ќќ¬≈ЌЌќ√ќ «ј –џ“»я ---
        // ≈сли с момента открыти€ прошло меньше 0.2 секунды - ничего не делаем.
        // Ёто предотвращает считывание того же нажати€ 'E', которое открыло записку.
        if (Time.time - openTimeStamp < 0.2f) return;
        // ---------------------------------------------------

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[NoteUIManager] »грок закрыл записку кнопкой.");
            CloseNote();
        }
    }

    public void OpenNote(string text)
    {
        if (notePanel == null || noteTextArea == null) return;

        noteTextArea.text = text;
        notePanel.SetActive(true);
        isNoteOpen = true;

        // --- «јѕќћ»Ќј≈ћ ¬–≈ћя ќ“ –џ“»я ---
        openTimeStamp = Time.time;
        // ---------------------------------

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[NoteUIManager] «аписка успешно открыта.");
    }

    public void CloseNote()
    {
        if (notePanel == null) return;

        notePanel.SetActive(false);
        isNoteOpen = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}