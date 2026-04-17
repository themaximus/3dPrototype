using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [Header("Настройки прочности")]
    public int maxHealth = 50;
    private int currentHealth;

    // --- ЗАДЕЛ НА БУДУЩЕЕ ---
    [Header("Эффекты разрушения")]
    [Tooltip("Префаб осколков, который появится на месте сломанного предмета (опционально)")]
    public GameObject brokenVersionPrefab;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Метод принимает урон и информацию о том, может ли оружие ломать предметы
    public void TakeDamage(int damage, bool canBreak)
    {
        // Если оружие не умеет ломать предметы, игнорируем урон
        if (!canBreak)
        {
            Debug.Log($"[Destructible] Оружие отскочило от {gameObject.name}. Нужен инструмент (лом/топор)!");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"[Destructible] {gameObject.name} получил {damage} урона. Прочность: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            BreakObject();
        }
    }

    private void BreakObject()
    {
        Debug.Log($"[Destructible] Объект {gameObject.name} РАЗРУШЕН!");

        // --- СОВМЕСТИМОСТЬ СО СКРИПТОМ ITEM PICKUP ---
        // Если это был предмет, который можно подобрать в инвентарь,
        // мы принудительно "гасим" его UI-подсветку перед удалением, 
        // чтобы текст с названием не завис на экране навсегда.
        ItemPickup pickupScript = GetComponent<ItemPickup>();
        if (pickupScript != null)
        {
            pickupScript.OnHoverExit();
        }
        // ---------------------------------------------

        // --- ЛОГИКА АНИМАЦИИ И ОСКОЛКОВ ---
        // Если в инспекторе назначен префаб сломанной версии (например, куча щепок), спавним его
        if (brokenVersionPrefab != null)
        {
            Instantiate(brokenVersionPrefab, transform.position, transform.rotation);
        }

        // (Альтернатива) Если разрушение — это анимация на самом объекте:
        // GetComponent<Animator>().SetTrigger("Destroy");
        // Destroy(gameObject, 2f); // Ждем 2 секунды, пока проиграется анимация, затем удаляем
        // return;

        // Удаляем целый объект со сцены
        Destroy(gameObject);
    }
}