using UnityEngine;

public class TipperRelay : MonoBehaviour
{
    [Header("Настройка Реле")]
    [Tooltip("Название тега для поиска вагонов (должно совпадать с тегом в Project Settings)")]
    public string targetTag = "Wagon";

    [Tooltip("Перетащите сюда контроллер TipperController с Tipper_Base")]
    public TipperController controller;

    // Unity вызывает этот метод, когда что-то входит в триггер
    private void OnTriggerEnter(Collider other)
    {
        // 1. Проверяем тег (как вы просили)
        if (other.CompareTag(targetTag))
        {
            // 2. Ищем WagonAnchor на корневом объекте
            WagonAnchor wagon = other.transform.root.GetComponent<WagonAnchor>();
            if (wagon != null)
            {
                // 3. Передаем событие в главный контроллер
                controller.AddWagon(wagon);
            }
        }
    }

    // Unity вызывает этот метод, когда что-то выходит из триггера
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            WagonAnchor wagon = other.transform.root.GetComponent<WagonAnchor>();
            if (wagon != null)
            {
                controller.RemoveWagon(wagon);
            }
        }
    }

    // Рисуем габаритный куб триггера в редакторе
    private void OnDrawGizmos()
    {
        if (GetComponent<Collider>() is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}