using UnityEngine;
using System;

public class StatController : MonoBehaviour
{
    [Header("Stats")]
    public CharacterStats characterStats; // ������ �� ScriptableObject

    [Header("Health")]
    private int currentHealth;

    public event Action OnDeath;

    void Awake()
    {
        // ������ �� ����� ������������ �������� �� ������ ������
        currentHealth = characterStats.maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " ������� " + damage + " �����. �������� " + currentHealth + " HP.");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " ����.");
        OnDeath?.Invoke();
    }
}