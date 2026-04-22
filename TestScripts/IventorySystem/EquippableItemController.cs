using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Универсальный контроллер для экипируемых предметов.
/// Обрабатывает нажатие ЛКМ, кулдауны и вызов нужного действия.
/// </summary>
public class EquippableItemController : MonoBehaviour
{
    public enum EquippableType
    {
        MeleeWeapon,    // Оружие ближнего боя (Лом, Топор)
        Consumable,     // Расходник (Еда, Аптечка)
        Custom          // Для уникальных предметов (Фонарик, Пульт, Рация)
    }

    [Header("Базовые настройки")]
    public EquippableType itemType = EquippableType.MeleeWeapon;

    [Tooltip("Ссылка на данные предмета (для оружия и расходников).")]
    public ItemData itemData;

    [Header("Настройки Оружия")]
    public LayerMask targetLayers;
    [Tooltip("Откуда пускаем луч удара. Если пусто - от главной камеры.")]
    public Transform attackOrigin;

    [Header("Кастомные события (Тип Custom)")]
    [Tooltip("Вызывается при срабатывании Animation Event 'PerformAction'")]
    public UnityEvent OnCustomActionPerformed;

    [Header("Звуки и Эффекты (Общие)")]
    public AudioClip actionSound;
    public ParticleSystem hitEffectPrefab;

    private Animator animator;
    private AudioSource audioSource;
    private Camera mainCam;
    private float nextActionTime = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCam = Camera.main;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (attackOrigin == null && mainCam != null) attackOrigin = mainCam.transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextActionTime)
        {
            TriggerAction();
        }
    }

    private void TriggerAction()
    {
        // Изменение здесь!
        // По умолчанию ставим 0, чтобы предметы типа Custom (фонарик) 
        // срабатывали без задержки, если у них нет аниматора.
        float cooldown = 0f;

        if (itemType == EquippableType.MeleeWeapon && itemData != null)
        {
            WeaponItemData wData = itemData as WeaponItemData;
            if (wData != null && wData.weaponStats != null)
            {
                cooldown = wData.weaponStats.attackSpeed;
            }
        }
        else if (itemType == EquippableType.Consumable)
        {
            // Для еды, если нужно, можно оставить кулдаун, например 1 секунду
            cooldown = 1f;
        }

        nextActionTime = Time.time + cooldown;

        if (animator != null)
        {
            animator.SetTrigger("Action");
        }
        else
        {
            PerformAction();
        }
    }

    // ВЫЗЫВАЕТСЯ ЧЕРЕЗ ANIMATION EVENT
    public void PerformAction()
    {
        if (actionSound != null) audioSource.PlayOneShot(actionSound);

        switch (itemType)
        {
            case EquippableType.MeleeWeapon:
                HandleMeleeAttack();
                break;

            case EquippableType.Consumable:
                HandleConsume();
                break;

            case EquippableType.Custom:
                // Вот здесь мы просто "дергаем" любые внешние скрипты!
                OnCustomActionPerformed?.Invoke();
                break;
        }
    }

    private void HandleMeleeAttack()
    {
        if (itemType != EquippableType.MeleeWeapon || itemData == null || attackOrigin == null) return;

        WeaponItemData wData = itemData as WeaponItemData;
        if (wData == null || wData.weaponStats == null) return;

        RaycastHit hit;
        if (Physics.Raycast(attackOrigin.position, attackOrigin.forward, out hit, wData.weaponStats.attackRange, targetLayers))
        {
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            DestructibleObject destructible = hit.collider.GetComponent<DestructibleObject>();
            if (destructible != null)
            {
                destructible.TakeDamage(wData.weaponStats.attackDamage, wData.weaponStats.canBreakObjects);
                return;
            }

            StatController stats = hit.collider.GetComponent<StatController>();
            if (stats != null)
            {
                stats.TakeDamage(wData.weaponStats.attackDamage);
            }
        }
    }

    private void HandleConsume()
    {
        if (itemType != EquippableType.Consumable || itemData == null) return;

        ConsumableItemData cData = itemData as ConsumableItemData;
        if (cData != null)
        {
            GameObject player = mainCam != null ? mainCam.transform.root.gameObject : null;
            if (player != null)
            {
                cData.Use(player);

                InventorySystem invSystem = player.GetComponent<InventorySystem>();
                if (invSystem != null)
                {
                    invSystem.RemoveActiveItem();
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (itemType == EquippableType.MeleeWeapon && attackOrigin != null && itemData != null)
        {
            WeaponItemData wData = itemData as WeaponItemData;
            if (wData != null && wData.weaponStats != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(attackOrigin.position, attackOrigin.forward * wData.weaponStats.attackRange);
            }
        }
    }
}