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
    public void EquipItem(ItemData itemData)
    {
        // 1. Уничтожаем старый предмет в руке (если он был)
        if (currentHandModel != null)
        {
            Destroy(currentHandModel);
            currentHandModel = null;
        }

        // 2. Если 'itemData' не null и у него есть префаб...
        if (itemData != null && itemData.handModelPrefab != null)
        {
            GameObject handModel = itemData.handModelPrefab;

            // --- ИСПРАВЛЕННЫЙ БЛОК СПАВНА ---
            // Создаем предмет, делая его дочерним для handContainer.
            // При таком способе Unity сохраняет родной поворот (Rotation) из префаба!
            currentHandModel = Instantiate(handModel, handContainer);

            // Сбрасываем позицию в ноль, чтобы предмет появился точно в центре камеры (в руках)
            currentHandModel.transform.localPosition = Vector3.zero;

            // Жестко фиксируем масштаб 1:1:1, чтобы избежать сплющивания или изменения размера
            currentHandModel.transform.localScale = Vector3.one;
            // ---------------------------------

            Debug.Log($"[EquipmentManager] Экипирован: {itemData.itemName}");
        }
        else
        {
            // Если мы передали null (слот пуст), то просто держим руки пустыми
            Debug.Log("[EquipmentManager] Руки убраны (слот пуст или нет модели)");
        }
    }
}