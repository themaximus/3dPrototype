using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WeaponController : MonoBehaviour
{
    [Header("Атака")]
    public int attackDamage = 25;
    public float attackRange = 1.5f;
    public Transform attackPoint;
    public LayerMask npcLayer;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (attackPoint == null)
        {
            attackPoint = this.transform;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Attack");
            PerformAttack();
        }
    }

    public void PerformAttack()
    {
        RaycastHit hit; // Используем RaycastHit для 3D

        // Используем Physics.Raycast для 3D-луча
        if (Physics.Raycast(attackPoint.position, attackPoint.right, out hit, attackRange, npcLayer))
        {
            Debug.DrawRay(attackPoint.position, attackPoint.right * attackRange, Color.green, 1f);

            StatController targetStats = hit.collider.GetComponent<StatController>();

            if (targetStats != null)
            {
                Debug.Log("🎯 Попадание (3D)! Наносим " + attackDamage + " урона цели: " + hit.collider.name);
                targetStats.TakeDamage(attackDamage);
            }
            else
            {
                Debug.Log("💥 Попали в 3D-объект " + hit.collider.name + ", но у него нет компонента StatController.");
            }
        }
        else
        {
            Debug.DrawRay(attackPoint.position, attackPoint.right * attackRange, Color.red, 1f);
            Debug.Log("💨 Промах (3D)! Луч ни во что не попал.");
        }
    }
}