using UnityEngine;

public class NoteObject : MonoBehaviour
{
    [Header("Настройки Записки")]
    [TextArea(5, 10)] // Делает большое поле для ввода текста в Инспекторе
    public string noteText = "Тут должен быть текст записки...";

    // Метод для вызова подсветки (опционально, если захотите добавить обводку)
    public void OnHoverEnter()
    {
        // Тут можно включить подсветку
    }

    public void OnHoverExit()
    {
        // Тут выключить
    }
}