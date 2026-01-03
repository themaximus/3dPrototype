using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Пустой дочерний объект камеры, в котором будет появляться оружие")]
    public Transform handContainer;

    // Ссылка на текущий созданный объект в руке
    private GameObject currentHandModel;

    /// <summary>
    /// Экипирует новый предмет (или убирает его)
    /// </summary>
    // --- ИЗМЕНЕНИЕ (1) ---
    // Меняем тип с WeaponItemData на ItemData
    public void EquipItem(ItemData itemData)
    {
        // 1. Уничтожаем старый предмет в руке (если он был)
        if (currentHandModel != null)
        {
            Destroy(currentHandModel);
            currentHandModel = null;
        }

        // --- ИЗМЕНЕНИЕ (2) ---
        // 2. Если 'itemData' не null и у него есть префаб...
        if (itemData != null && itemData.handModelPrefab != null)
        {
            // ...создаем новый префаб
            GameObject handModel = itemData.handModelPrefab;

            // 3. Создаем (Instantiate) префаб внутри 'handContainer'
            currentHandModel = Instantiate(handModel, handContainer.position, handContainer.rotation, handContainer);

            Debug.Log($"[EquipmentManager] Экипирован: {itemData.itemName}");
        }
        else
        {
            // Если мы передали null (слот пуст), то просто держим руки пустыми
            Debug.Log("[EquipmentManager] Руки убраны (слот пуст или нет модели)");
        }
        // -------------------------
    }
}