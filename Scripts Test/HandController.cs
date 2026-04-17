using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HandController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public WeaponData weaponData;

    [Header("Setup")]
    [Tooltip("Точка, из которой вылетает луч атаки. Луч идет вдоль оси X (Red axis) этой точки.")]
    public Transform attackPoint;

    [Tooltip("Слои, по которым проходит урон (NPC, Разрушаемые объекты и т.д.)")]
    public LayerMask targetLayers;

    [Header("Debug Settings (Gizmos)")]
    [Tooltip("Показывать ли точку атаки и радиус в редакторе?")]
    public bool showAttackGizmos = true;

    [Tooltip("Цвет, которым будет рисоваться отладочный луч и сфера")]
    public Color gizmoColor = Color.cyan;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;
    private AudioSource audioSource;

    private Animator animator;
    private float nextAttackTime = 0f;

    void Start()
    {
        // Проверка на то, что WeaponData назначен
        if (weaponData == null)
        {
            Debug.LogError("WeaponData не назначен в инспекторе! Пожалуйста, добавьте ассет.");
            this.enabled = false;
            return;
        }

        animator = GetComponent<Animator>();

        // Если attackPoint не назначен, используем сам объект оружия как точку
        if (attackPoint == null)
        {
            attackPoint = this.transform;
        }

        // Находим или добавляем AudioSource для звуков
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Логика нажатия на ЛКМ с кулдауном
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + weaponData.attackSpeed;
            animator.SetTrigger("Attack");
        }
    }

    // Эта функция вызывается из анимации (через Animation Event)
    public void PerformAttack()
    {
        Debug.Log("🗡️ [Отладка] Метод PerformAttack сработал! Пускаем луч...");

        RaycastHit hit;

        // Пускаем луч из attackPoint вдоль его локальной оси X (right)
        if (Physics.Raycast(attackPoint.position, attackPoint.right, out hit, weaponData.attackRange, targetLayers))
        {
            Debug.DrawRay(attackPoint.position, attackPoint.right * weaponData.attackRange, Color.green, 1f);
            Debug.Log("🎯 [Отладка] Луч врезался в коллайдер: " + hit.collider.gameObject.name);

            // 1. Проверка на живых NPC
            StatController targetStats = hit.collider.GetComponent<StatController>();
            if (targetStats != null)
            {
                targetStats.TakeDamage(weaponData.attackDamage);
                PlayHitEffects(hit);
                return; // Успешно ударили NPC, выходим из метода
            }

            // 2. Проверка на разрушаемые объекты (Ящики, столы и т.д.)
            DestructibleObject destructible = hit.collider.GetComponent<DestructibleObject>();
            if (destructible != null)
            {
                destructible.TakeDamage(weaponData.attackDamage, weaponData.canBreakObjects);
                PlayHitEffects(hit);
                return; // Успешно ударили предмет, выходим из метода
            }
        }
        else
        {
            // Луч вылетел, но ни во что не врезался
            Debug.DrawRay(attackPoint.position, attackPoint.right * weaponData.attackRange, Color.red, 1f);
            Debug.Log("💨 [Отладка] Луч ушел в молоко (ничего не задел или слой не совпал).");
        }
    }

    // Метод для отрисовки эффектов и звука
    private void PlayHitEffects(RaycastHit hit)
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }

        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }

    // --- МЕТОД ДЛЯ ОТОБРАЖЕНИЯ ОТЛАДКИ (GIZMOS) В РЕДАКТОРЕ ---
    private void OnDrawGizmos()
    {
        if (attackPoint != null && weaponData != null && showAttackGizmos)
        {
            Gizmos.color = gizmoColor;

            // Рисуем маленькую сферу в начале луча
            Gizmos.DrawWireSphere(attackPoint.position, 0.05f);

            // Рисуем сам луч атаки (невидимый луч Raycast)
            Vector3 startPos = attackPoint.position;
            Vector3 endPos = startPos + attackPoint.right * weaponData.attackRange;
            Gizmos.DrawLine(startPos, endPos);

            // Рисуем сферу на конце луча, показывая максимальную дальность
            Gizmos.DrawWireSphere(endPos, 0.1f);
        }
    }
}