using UnityEngine;

public class DestructibleObject : MonoBehaviour, IInteractable
{
    [Header("Настройки прочности")]
    public int maxHealth = 50;
    private int currentHealth;

    [Header("UI Подсказка")]
    public string customHoverText = "Заколочено";
    public bool showWorldUI = true;

    [Header("Эффекты разрушения")]
    public GameObject brokenVersionPrefab;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Физический урон (оружием)
    public void TakeDamage(int damage, bool canBreak)
    {
        if (!canBreak) return;

        currentHealth -= damage;
        if (currentHealth <= 0) BreakObject();
    }

    private void BreakObject()
    {
        OnHoverExit(); // Прячем UI перед смертью

        if (brokenVersionPrefab != null)
        {
            Instantiate(brokenVersionPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }

    // --- ИНТЕРФЕЙС IInteractable ---

    public void Interact(GameObject player)
    {
        Debug.Log($"[Destructible] Пытаюсь открыть {gameObject.name} руками, но он заколочен!");
    }

    public void SecondaryInteract(GameObject player)
    {
        // Нельзя взять заколоченный ящик в руки. Оставляем пустым.
    }

    public void OnHoverEnter()
    {
        if (!showWorldUI) return;
        // Здесь можно показать UI подсказку
    }

    public void OnHoverExit()
    {
        if (!showWorldUI) return;
        // Здесь можно скрыть UI подсказку
    }
}