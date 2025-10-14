using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(StatController))]
[RequireComponent(typeof(Animator))]
public class NpcDeathHandler : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private Collider npcCollider;
    private NpcAI npcAI; // Если у тебя есть скрипт преследования

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        npcCollider = GetComponent<Collider>();
        npcAI = GetComponent<NpcAI>();

        // Подписываемся на смерть
        GetComponent<StatController>().OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} проигрывает анимацию смерти");

        // Остановить любое движение
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Отключаем AI, чтобы не двигался и не атаковал
        if (npcAI != null)
        {
            npcAI.enabled = false;
        }

        // Отключаем коллайдер (чтобы не мешал атаке после смерти)
        if (npcCollider != null)
        {
            npcCollider.enabled = false;
        }

        // Запускаем анимацию
        animator.SetTrigger("Death");
    }
}
