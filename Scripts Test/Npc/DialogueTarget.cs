using UnityEngine;
using Yarn.Unity;

public class DialogueTarget : MonoBehaviour, IInteractable
{
    [Header("Настройки диалога")]
    [Tooltip("Имя узла (title) в Yarn-файле для запуска")]
    public string dialogueNode = "Start";

    private DialogueRunner dialogueRunner;

    void Start()
    {
        // Ищем на сцене главный менеджер диалогов
        dialogueRunner = FindObjectOfType<DialogueRunner>();

        if (dialogueRunner == null)
        {
            Debug.LogError($"[DialogueTarget] На сцене нет DialogueRunner! {gameObject.name} не сможет начать диалог.");
        }
    }

    // --- Реализация строгого договора IInteractable ---

    // 1. Метод взаимодействия (теперь принимает объект игрока, как того требует интерфейс)
    public void Interact(GameObject player)
    {
        if (dialogueRunner == null) return;

        // Защита от спама: если диалог УЖЕ запущен, игнорируем нажатие
        if (dialogueRunner.IsDialogueRunning)
        {
            return;
        }

        Debug.Log($"Начинаем диалог: {dialogueNode}");
        dialogueRunner.StartDialogue(dialogueNode);
    }

    // 2. Метод при наведении луча
    public void OnHoverEnter()
    {
        // Здесь можно включать подсветку (Outline) для персонажа
        // или выводить на экран подсказку "[F] Говорить"
    }

    // 3. Метод при отводе луча
    public void OnHoverExit()
    {
        // Здесь мы выключаем подсветку или прячем UI с подсказкой
    }
}