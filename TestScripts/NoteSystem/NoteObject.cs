using UnityEngine;

// Заменяет твой старый NoteObject. Теперь луч игрока будет сам видеть записки.
// Старый скрипт NoteInteractor.cs можно удалить из проекта.
public class NoteObject : MonoBehaviour, IInteractable
{
    [Header("Содержимое записки")]
    public string noteTitle = "Таинственная записка";

    [TextArea(5, 10)]
    public string noteContent = "Текст записки...";

    public void Interact(GameObject player)
    {
        // Открываем UI Записки
        if (NoteUIManager.Instance != null)
        {
            // Метод называется OpenNote и принимает одну строку. 
            // Объединяем заголовок и текст с помощью переноса строки (\n\n)
            string fullText = noteTitle + "\n\n" + noteContent;
            NoteUIManager.Instance.OpenNote(fullText);
        }
    }

    public void SecondaryInteract(GameObject player)
    {
        // Если хочешь, чтобы записку можно было брать в руки как физический объект, 
        // скопируй сюда код из SecondaryInteract скрипта ItemPickup.
    }

    public void OnHoverEnter()
    {
        // Подсветить записку
    }

    public void OnHoverExit()
    {
        // Убрать подсветку
    }
}