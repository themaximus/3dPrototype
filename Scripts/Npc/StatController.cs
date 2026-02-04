using UnityEngine;
using System;
using System.Collections.Generic;

public class StatController : MonoBehaviour
{
    [Header("Data")]
    public CharacterStats characterStats;

    // Словарь: Тип стата -> Текущее значение
    private Dictionary<StatType, float> currentValues = new Dictionary<StatType, float>();
    // Словарь: Тип стата -> Максимальное значение (для удобства)
    private Dictionary<StatType, float> maxValues = new Dictionary<StatType, float>();

    // Событие: Тип, Текущее, Максимум
    public event Action<StatType, float, float> OnStatChanged;
    public event Action OnDeath;

    // Свойство для совместимости с твоими старыми скриптами
    public int CurrentHealth => (int)GetStatValue(StatType.Health);

    void Awake()
    {
        if (characterStats == null) return;

        // Инициализация статов из ScriptableObject
        foreach (var statDef in characterStats.statsConfig)
        {
            currentValues[statDef.type] = statDef.maxValue;
            maxValues[statDef.type] = statDef.maxValue;
        }
    }

    void Start()
    {
        // При старте обновляем UI для всех статов
        foreach (var statDef in characterStats.statsConfig)
        {
            NotifyStatChanged(statDef.type);
        }
    }

    void Update()
    {
        // 1. Естественное уменьшение потребностей (Голод, Жажда и т.д.)
        foreach (var statDef in characterStats.statsConfig)
        {
            if (statDef.depletionRate > 0)
            {
                ModifyStat(statDef.type, -statDef.depletionRate * Time.deltaTime);
            }
        }

        // 2. Последствия (Например: Если Голод = 0, отнимаем Здоровье)
        HandleDepletionPenalty(StatType.Hunger, 1f); // -1 ХП в сек, если голоден
        HandleDepletionPenalty(StatType.Thirst, 2f); // -2 ХП в сек, если жажда
    }

    // Исправленная версия ModifyStat
    public void ModifyStat(StatType type, float amount)
    {
        if (!currentValues.ContainsKey(type)) return;

        float current = currentValues[type];
        float max = maxValues[type];

        float newVal = Mathf.Clamp(current + amount, 0, max);

        // --- ИСПРАВЛЕНИЕ: Убрали жесткую проверку > 0.01f ---
        // Просто проверяем, изменилось ли значение хоть немного
        if (newVal != current)
        {
            currentValues[type] = newVal;
            NotifyStatChanged(type);

            // Проверка на смерть (только для Здоровья)
            if (type == StatType.Health && newVal <= 0)
            {
                Die();
            }
        }
    }

    public float GetStatValue(StatType type)
    {
        return currentValues.ContainsKey(type) ? currentValues[type] : 0f;
    }

    // Вспомогательный метод: Наносит урон здоровью, если какой-то стат на нуле
    private void HandleDepletionPenalty(StatType checkType, float damagePerSecond)
    {
        if (GetStatValue(checkType) <= 0)
        {
            ModifyStat(StatType.Health, -damagePerSecond * Time.deltaTime);
        }
    }

    private void NotifyStatChanged(StatType type)
    {
        if (currentValues.ContainsKey(type))
        {
            OnStatChanged?.Invoke(type, currentValues[type], maxValues[type]);
        }
    }

    // --- МЕТОДЫ СОВМЕСТИМОСТИ (Чтобы не ломать Weapon и AI) ---
    public void TakeDamage(int damage)
    {
        ModifyStat(StatType.Health, -damage);
        Debug.Log($"{name} получил урон: {damage}");
    }

    public void Heal(int amount)
    {
        ModifyStat(StatType.Health, amount);
    }
    // ----------------------------------------------------------

    private void Die()
    {
        Debug.Log($"{name} погиб.");
        OnDeath?.Invoke();
    }
}