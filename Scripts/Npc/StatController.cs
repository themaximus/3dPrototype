using UnityEngine;
using System;

public class StatController : MonoBehaviour
{
    [Header("Stats")]
    public CharacterStats characterStats; // Ссылка на ScriptableObject

    [Header("Health")]
    private int currentHealth;

    public event Action OnDeath;

    // --- НОВОЕ СОБЫТИЕ ---
    // Будет отправлять (Текущее ХП, Макс. ХП) всем, кто слушает
    public event Action<int, int> OnHealthChanged;
    // ---------------------

    // --- Свойство для UI (из прошлых шагов) ---
    public int CurrentHealth => currentHealth;

    void Awake()
    {
        // Теперь мы берем максимальное здоровье из нашего ассета
        currentHealth = characterStats.maxHealth;
    }

    void Start()
    {
        // --- НОВЫЙ ВЫЗОВ ---
        // В самом начале, сообщаем UI, какое у нас стартовое ХП
        OnHealthChanged?.Invoke(currentHealth, characterStats.maxHealth);
        // -------------------
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

        // --- НОВЫЙ ВЫЗОВ ---
        // Сообщаем UI, что ХП изменилось
        OnHealthChanged?.Invoke(currentHealth, characterStats.maxHealth);
        // -------------------
    }

    // --- Метод лечения (из прошлых шагов) ---
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return; // Нельзя лечить мертвых

        currentHealth += amount;

        // Убедимся, что здоровье не превышает максимум
        if (currentHealth > characterStats.maxHealth)
        {
            currentHealth = characterStats.maxHealth;
        }

        Debug.Log(gameObject.name + " восстановил " + amount + " ХП. Текущее здоровье: " + currentHealth);

        // --- НОВЫЙ ВЫЗОВ ---
        // Сообщаем UI, что ХП изменилось
        OnHealthChanged?.Invoke(currentHealth, characterStats.maxHealth);
        // -------------------
    }
    // ---------------------

    private void Die()
    {
        Debug.Log(gameObject.name + " умер.");
        OnDeath?.Invoke();
    }
}