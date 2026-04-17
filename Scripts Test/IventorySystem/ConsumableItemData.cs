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

    public override void Use(GameObject user)
    {
        StatController stats = user.GetComponent<StatController>();

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
    }

    private void OnValidate()
    {
        itemType = ItemType.Consumable;
        isStackable = true;
        if (maxStackSize <= 1) maxStackSize = 10;
    }
}