using UnityEngine;

[CreateAssetMenu(fileName = "New Character Stats", menuName = "Stats/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("Movement")]
    public float speed = 5.0f;
    public float sprintSpeedMultiplier = 1.5f;
    public float crouchSpeedMultiplier = 0.5f;

    [Header("Jumping")]
    public float jumpHeight = 1.0f;

    // --- днаюбкеммши акнй ---
    [Header("Combat")]
    public int attackDamage = 10;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    // -------------------------
}