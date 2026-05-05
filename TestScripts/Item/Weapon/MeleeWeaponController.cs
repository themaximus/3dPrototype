using UnityEngine;

public class MeleeWeaponController : MonoBehaviour
{
    [Header("Настройки оружия")]
    public WeaponData weaponData;
    public LayerMask targetLayers; // Слои NPC и разрушаемых объектов

    [Header("Ссылки")]
    [Tooltip("Если не назначено, будет использоваться главная камера")]
    public Transform attackOrigin;
    private Animator animator;
    private Camera mainCam;

    private float nextAttackTime = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCam = Camera.main;

        // Если точка начала атаки не назначена вручную, используем камеру
        if (attackOrigin == null && mainCam != null)
        {
            attackOrigin = mainCam.transform;
        }

        if (weaponData == null)
        {
            Debug.LogError($"[MeleeWeaponController] WeaponData не назначен на {gameObject.name}!", this);
            this.enabled = false;
        }
    }

    void Update()
    {
        // Проверяем нажатие ЛКМ и кулдаун атаки
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            Attack();
        }
    }

    private void Attack()
    {
        nextAttackTime = Time.time + weaponData.attackSpeed;

        if (animator != null)
        {
            // Запускаем анимацию. Убедись, что в анимации есть Animation Event, вызывающий PerformAttack
            animator.SetTrigger("Attack");
        }
        else
        {
            // Если аниматора нет, наносим урон мгновенно
            PerformAttack();
        }
    }

    // Этот метод вызывается через Animation Event в окне Animation
    public void PerformAttack()
    {
        if (weaponData == null || attackOrigin == null) return;

        RaycastHit hit;
        // Пускаем луч точно так же, как в PlayerInteractor: из центра вперед
        if (Physics.Raycast(attackOrigin.position, attackOrigin.forward, out hit, weaponData.attackRange, targetLayers))
        {
            Debug.Log($"[Melee] Попадание в: {hit.collider.name}");

            // 1. Проверяем на разрушаемый объект (ящики, доски)
            DestructibleObject destructible = hit.collider.GetComponent<DestructibleObject>();
            if (destructible != null)
            {
                destructible.TakeDamage(weaponData.attackDamage, weaponData.canBreakObjects);
                return; // Удар поглощен объектом
            }

            // 2. Проверяем на NPC или Игрока (через StatController)
            StatController stats = hit.collider.GetComponent<StatController>();
            if (stats != null)
            {
                stats.TakeDamage(weaponData.attackDamage);
            }
        }
    }

    // Визуализация луча атаки в редакторе (только во время работы)
    void OnDrawGizmos()
    {
        if (attackOrigin == null || weaponData == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(attackOrigin.position, attackOrigin.forward * weaponData.attackRange);
    }
}