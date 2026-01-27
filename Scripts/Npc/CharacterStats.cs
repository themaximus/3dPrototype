using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StatDefinition
{
    public string name = "New Stat"; // Имя для удобства (Health, Hunger)
    public StatType type;
    public float maxValue = 100f;
    [Tooltip("Сколько единиц отнимается каждую секунду")]
    public float depletionRate = 0f;

    [Header("UI Settings")]
    [Tooltip("Как отображать стат? {0}=текущее, {1}=макс. Пример: '{0:0}%'")]
    public string format = "{0:0} / {1:0}"; // <-- НОВОЕ ПОЛЕ
}

[CreateAssetMenu(fileName = "New Character Stats", menuName = "Stats/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Настройки Характеристик")]
    public List<StatDefinition> statsConfig = new List<StatDefinition>();

    // Остальные параметры (скорость, атака и т.д.) оставляем без изменений...
    [Header("Movement")]
    public float speed = 5.0f;
    public float sprintSpeedMultiplier = 1.5f;
    public float crouchSpeedMultiplier = 0.5f;
    public float jumpHeight = 1.0f;

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
}