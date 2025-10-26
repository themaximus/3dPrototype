using UnityEngine;
using System;

public class StatController : MonoBehaviour
{
    [Header("Stats")]
    public CharacterStats characterStats; // Ссылка на ScriptableObject

    [Header("Health")]
    private int currentHealth;

    public event Action OnDeath;

    void Awake()
    {
        // Теперь мы берем максимальное здоровье из нашего ассета
        currentHealth = characterStats.maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " получил " + damage + " урона. Осталось " + currentHealth + " HP.");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " умер.");
        OnDeath?.Invoke();
    }
}