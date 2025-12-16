using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class QuickSlotInputHandler : MonoBehaviour
{
    [Tooltip("Ссылка на 'мозг' инвентаря. Должна найтись автоматически.")]
    private InventorySystem inventorySystem;

    // Массив клавиш для быстрого доступа.
    // KeyCode.Alpha1 - это '1' над 'Q', не на Numpad
    private KeyCode[] quickSlotKeys =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9,
    };

    void Awake()
    {
        // Автоматически находим 'мозг' на этом же объекте
        inventorySystem = GetComponent<InventorySystem>();
    }

    void Update()
    {
        // --- 1. Проверяем нажатие клавиш 1-9 ---
        for (int i = 0; i < quickSlotKeys.Length; i++)
        {
            // Если нажата клавиша (например, '1')...
            if (Input.GetKeyDown(quickSlotKeys[i]))
            {
                // (Логика выбора слота)
                if (i < inventorySystem.QuickSlots.Count)
                {
                    inventorySystem.SetActiveQuickSlot(i);
                    return;
                }
            }
        }

        // --- 2. Проверяем колесико мыши ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            // (Логика скролла)
            int currentIndex = inventorySystem.GetActiveQuickSlotIndex();
            if (scroll > 0f) currentIndex--;
            else if (scroll < 0f) currentIndex++;

            int slotCount = inventorySystem.QuickSlots.Count;
            if (currentIndex < 0) currentIndex = slotCount - 1;
            if (currentIndex >= slotCount) currentIndex = 0;

            inventorySystem.SetActiveQuickSlot(currentIndex);
        }

        // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
        // 3. Проверяем нажатие ПКМ для "использования"
        // (Мы НЕ проверяем ЛКМ (MouseButtonDown(0)),
        // т.к. ЛКМ должен обрабатываться самим префабом оружия,
        // например, в твоем HandController.cs)
        if (Input.GetMouseButtonDown(1)) // 1 = Правая кнопка мыши
        {
            // Говорим "мозгу": используй то, что сейчас в руках
            inventorySystem.UseActiveQuickSlotItem();
        }
        // -------------------------
    }
}