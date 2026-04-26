using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct StatModifier
{
    public StatType statType;
    public float amount; // Может быть отрицательным (например, отравленная еда)
}

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable Item")]
public class ConsumableItemData : ItemData
{
    [Header("Эффекты предмета")]
    [Tooltip("Список характеристик, которые изменит этот предмет")]
    public List<StatModifier> effects = new List<StatModifier>();

    [Header("Мусор / Пустая упаковка")]
    [Tooltip("Префаб, который появится после использования (например, пустая бутылка). Можно оставить пустым.")]
    public GameObject emptyPrefab;

    public override void Use(GameObject user)
    {
        StatController stats = user.GetComponentInChildren<StatController>();

        if (stats != null)
        {
            Debug.Log($"Потребляем {itemName}...");
            foreach (var effect in effects)
            {
                stats.ModifyStat(effect.statType, effect.amount);
                Debug.Log($" > {effect.statType}: {effect.amount}");
            }
        }
        else
        {
            Debug.LogWarning($"На {user.name} нет StatController!");
        }

        // --- НОВАЯ ЛОГИКА: Спавн пустой упаковки ---
        if (emptyPrefab != null)
        {
            // Пытаемся найти инвентарь игрока, чтобы узнать откуда выбрасывать мусор (из dropPoint)
            InventorySystem invSystem = user.GetComponentInChildren<InventorySystem>();
            if (invSystem == null) invSystem = FindObjectOfType<InventorySystem>();

            if (invSystem != null && invSystem.dropPoint != null)
            {
                // Создаем пустую упаковку в точке выброса предметов
                Instantiate(emptyPrefab, invSystem.dropPoint.position, invSystem.dropPoint.rotation);
            }
            else
            {
                // Если dropPoint нет, просто спавним прямо на месте игрока
                Instantiate(emptyPrefab, user.transform.position, user.transform.rotation);
            }
        }
        // -------------------------------------------
    }

    private void OnValidate()
    {
        itemType = ItemType.Consumable;
        isStackable = true;
        if (maxStackSize <= 1) maxStackSize = 10;
    }
}