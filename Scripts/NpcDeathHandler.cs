using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(StatController))]
[RequireComponent(typeof(Animator))]
public class NpcDeathHandler : MonoBehaviour
{
    private Animator animator;
    private Collider npcCollider;
    private NpcAI npcAI;

    void Awake()
    {
        animator = GetComponent<Animator>();
        npcCollider = GetComponent<Collider>();
        npcAI = GetComponent<NpcAI>();

        GetComponent<StatController>().OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} проигрывает анимацию смерти");

        // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
        // Вызываем централизованный метод смерти в NpcAI
        if (npcAI != null)
        {
            npcAI.InitiateDeath();
        }

        // Отключаем коллайдер, чтобы игрок мог пройти сквозь мертвое тело
        if (npcCollider != null)
        {
            npcCollider.enabled = false;
        }

        // NavMeshAgent отключается внутри InitiateDeath, так что здесь его трогать не нужно.

        // Запускаем анимацию
        animator.SetTrigger("Death");
    }
}