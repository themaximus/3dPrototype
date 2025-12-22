using UnityEngine;

[RequireComponent(typeof(Animator))]
// --- ИЗМЕНЕНИЕ (1) ---
// Класс теперь называется HandController, чтобы соответствовать
// имени файла "HandController.cs".
public class HandController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public WeaponData weaponData;

    [Header("Setup")]
    public Transform attackPoint;
    public LayerMask npcLayer;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;
    private AudioSource audioSource;

    private Animator animator;
    private float nextAttackTime = 0f;

    void Start()
    {
        if (weaponData == null)
        {
            Debug.LogError("WeaponData не назначен в инспекторе! Пожалуйста, добавьте ассет.");
            this.enabled = false;
            return;
        }

        animator = GetComponent<Animator>();
        if (attackPoint == null)
        {
            attackPoint = this.transform;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + weaponData.attackSpeed;

            // --- ИЗМЕНЕНИЕ ЗДЕСЬ --- (Логика из твоего файла)
            // Теперь мы ТОЛЬКО запускаем анимацию.
            // Вызов PerformAttack() отсюда убран.
            animator.SetTrigger("Attack");
        }
    }

    // Эта функция теперь будет вызываться СОБЫТИЕМ из анимации
    public void PerformAttack()
    {
        RaycastHit hit;

        if (Physics.Raycast(attackPoint.position, attackPoint.right, out hit, weaponData.attackRange, npcLayer))
        {
            Debug.DrawRay(attackPoint.position, attackPoint.right * weaponData.attackRange, Color.green, 1f);

            StatController targetStats = hit.collider.GetComponent<StatController>();

            if (targetStats != null)
            {
                Debug.Log("🎯 АНИМАЦИОННОЕ ПОПАДАНИЕ! Наносим " + weaponData.attackDamage + " урона.");
                targetStats.TakeDamage(weaponData.attackDamage);

                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }

                if (hitSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }
            }
        }
        else
        {
            Debug.DrawRay(attackPoint.position, attackPoint.right * weaponData.attackRange, Color.red, 1f);
        }
    }
}