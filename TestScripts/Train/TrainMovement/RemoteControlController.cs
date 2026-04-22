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

        // 2. Синхронизируем начальное состояние
        currentGear = targetLocomotive.throttleInput;

        // 3. ПОДПИСЫВАЕМСЯ на событие
        // Как только локомотив скажет "Я сменил передачу", сработает метод OnLocomotiveGearChanged
        targetLocomotive.OnGearChanged += OnLocomotiveGearChanged;

        Debug.Log($"[Remote] 📶 Пульт подключен к {targetLocomotive.name}.");
    }

    // Важно отписываться при уничтожении, чтобы не было ошибок
    void OnDestroy()
    {
        if (targetLocomotive != null)
        {
            targetLocomotive.OnGearChanged -= OnLocomotiveGearChanged;
        }
    }

    // Этот метод вызывается АВТОМАТИЧЕСКИ, когда локомотив шлет сигнал
    private void OnLocomotiveGearChanged(int newGear)
    {
        currentGear = newGear;
        Debug.Log($"[Remote] 🔄 Синхронизация с локомотивом: {currentGear}");
    }

    void Update()
    {
        if (targetLocomotive == null) return;

        // В Update остался ТОЛЬКО ввод игрока
        // Никаких проверок состояния локомотива здесь больше нет

        if (Input.GetMouseButtonDown(0))
        {
            ChangeGear(1);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            ChangeGear(-1);
        }
    }

    void ChangeGear(int direction)
    {
        int newGear = currentGear + direction;
        newGear = Mathf.Clamp(newGear, -1, 1);

        if (newGear != currentGear)
        {
            // Меняем локально
            currentGear = newGear;

            // Отправляем на локомотив (используем throttleInput напрямую, 
            // так как мы - источник команды, нам не нужно событие обратно)
            targetLocomotive.throttleInput = currentGear;

            // Логи
            string status = "";
            if (currentGear == 1) status = "ВПЕРЕД ⬆️";
            else if (currentGear == 0) status = "НЕЙТРАЛЬ ⏸️";
            else if (currentGear == -1) status = "НАЗАД ⬇️";

            Debug.Log($"[Remote] Команда отправлена: {status}");
        }
    }
}