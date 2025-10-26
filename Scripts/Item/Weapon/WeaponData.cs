using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Data", menuName = "Stats/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Attack Settings")]
    public int attackDamage = 25;
    public float attackRange = 1.5f;

    [Tooltip("Время между атаками в секундах. 0.5 = 2 атаки в секунду.")]
    public float attackSpeed = 0.8f; // <-- НОВОЕ ПОЛЕ
}