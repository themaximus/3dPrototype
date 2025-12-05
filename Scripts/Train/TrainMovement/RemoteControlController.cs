using UnityEngine;

public class RemoteControlController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private LocomotiveController targetLocomotive;
    [SerializeField] private int currentGear = 0; // -1 = Назад, 0 = Нейтраль, 1 = Вперед

    void Start()
    {
        // 1. Ищем локомотив на сцене
        targetLocomotive = FindObjectOfType<LocomotiveController>();

        if (targetLocomotive == null)
        {
            Debug.LogError("[Remote] ❌ Локомотив не найден на сцене!");
            this.enabled = false;
            return;
        }

        // Синхронизируемся с текущим состоянием поезда
        currentGear = targetLocomotive.throttleInput;
        Debug.Log($"[Remote] 📶 Пульт подключен к {targetLocomotive.name}. Текущая передача: {currentGear}");
    }

    void Update()
    {
        if (targetLocomotive == null) return;

        // 2. Управление кнопками мыши

        // ЛЕВАЯ КНОПКА (0) -> Повысить передачу (Вперед)
        if (Input.GetMouseButtonDown(0))
        {
            ChangeGear(1);
        }
        // ПРАВАЯ КНОПКА (1) -> Понизить передачу (Назад)
        else if (Input.GetMouseButtonDown(1))
        {
            ChangeGear(-1);
        }
    }

    void ChangeGear(int direction)
    {
        int newGear = currentGear + direction;

        // Ограничиваем значения от -1 до 1
        newGear = Mathf.Clamp(newGear, -1, 1);

        if (newGear != currentGear)
        {
            currentGear = newGear;

            // 3. Отправляем команду на Локомотив
            targetLocomotive.throttleInput = currentGear;

            // Обратная связь в консоль
            string status = "";
            if (currentGear == 1) status = "ВПЕРЕД ⬆️";
            else if (currentGear == 0) status = "НЕЙТРАЛЬ ⏸️";
            else if (currentGear == -1) status = "НАЗАД ⬇️";

            Debug.Log($"[Remote] Передача изменена: {status}");

            // Сюда можно добавить звук "клика"
            // GetComponent<AudioSource>()?.Play();
        }
    }
}