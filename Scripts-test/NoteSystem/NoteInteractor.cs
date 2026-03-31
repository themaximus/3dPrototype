using UnityEngine;

public class NoteInteractor : MonoBehaviour
{
    [Header("Настройки")]
    public float interactionDistance = 3f;
    public LayerMask interactionLayer;
    public KeyCode interactKey = KeyCode.E;

    [Header("Ссылки")]
    public Transform playerCamera;

    void Start()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>()?.transform;
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (playerCamera == null)
        {
            Debug.LogError("[NoteInteractor] КРИТИЧЕСКАЯ ОШИБКА: Камера не найдена! Скрипт отключен.");
            this.enabled = false;
        }
        else
        {
            Debug.Log($"[NoteInteractor] Камера найдена: {playerCamera.name}");
        }
    }

    void Update()
    {
        // Проверка Менеджера
        if (NoteUIManager.Instance == null)
        {
            // Спамим ошибкой, только если нажали E, чтобы не засорять консоль каждый кадр
            if (Input.GetKeyDown(interactKey))
                Debug.LogError("[NoteInteractor] ОШИБКА: NoteUIManager не найден в сцене! Вы забыли добавить его на Canvas?");
            return;
        }

        if (NoteUIManager.Instance.isNoteOpen) return;

        RaycastHit hit;

        // Рисуем луч в редакторе для наглядности (Красный - промах, Зеленый - попадание)
        Debug.DrawRay(playerCamera.position, playerCamera.forward * interactionDistance, Color.red);

        // ВАЖНО: QueryTriggerInteraction.Ignore чтобы игнорировать триггеры (траву, зоны)
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, interactionDistance, interactionLayer, QueryTriggerInteraction.Ignore))
        {
            // Если луч во что-то попал - рисуем зеленый луч
            Debug.DrawLine(playerCamera.position, hit.point, Color.green);

            // Проверяем, есть ли записка
            NoteObject note = hit.collider.GetComponent<NoteObject>();

            if (note != null)
            {
                // Мы смотрим на записку!
                if (Input.GetKeyDown(interactKey))
                {
                    Debug.Log($"[NoteInteractor] Нажата E. Пытаюсь прочитать записку: {hit.collider.name}");
                    NoteUIManager.Instance.OpenNote(note.noteText);
                }
            }
            else
            {
                // Мы смотрим на что-то, но это не записка. 
                // Раскомментируйте строку ниже, если хотите видеть, во что упирается луч (например, в стену или траву)
                // Debug.Log($"[NoteInteractor] Луч уперся в: {hit.collider.name}");
            }
        }
    }
}