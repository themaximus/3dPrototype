using UnityEngine;
using UnityEngine.AI;

public class ChaseState : State
{
    private float chaseTimer;
    private float pathUpdateRate = 0.2f; // Как часто обновляем путь

    public ChaseState(NpcAI npc, NavMeshAgent agent, Animator animator, Transform player)
        : base(npc, agent, animator, player) { }

    public override void Enter()
    {
        // При входе в состояние преследования, разрешаем агенту двигаться
        agent.isStopped = false;
        chaseTimer = 0f;
    }

    public override void Update()
    {
        // --- Логика перехода в другие состояния ---

        // 1. Если потеряли игрока из виду или он вышел из зоны...
        if (!npc.IsPlayerInZone() || !npc.HasLineOfSight())
        {
            // ...переключаемся в состояние ожидания
            npc.ChangeState(npc.idleState);
            return; // Выходим, чтобы не выполнять остальную логику
        }

        // 2. Если добежали до игрока и можем атаковать...
        float distanceToPlayer = Vector3.Distance(agent.transform.position, player.position);

        // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
        // Было: if (distanceToPlayer <= npc.attackDistance)
        if (distanceToPlayer <= npc.stats.attackRange) // Стало: используем статы
        {
            // ...переключаемся в состояние атаки
            npc.ChangeState(npc.attackState);
            return; // Выходим
        }

        // --- Основная логика состояния ---
        // Обновляем путь к игроку не каждый кадр, а с заданной частотой
        chaseTimer += Time.deltaTime;
        if (chaseTimer >= pathUpdateRate)
        {
            agent.SetDestination(player.position);
            chaseTimer = 0f;
        }

        // Обновляем анимацию бега
        animator.SetFloat("Speed", agent.velocity.magnitude);

        // Плавно поворачиваемся в сторону игрока
        LookAtPlayer();
    }

    public override void Exit()
    {
        // При выходе из этого состояния останавливаем агента
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetFloat("Speed", 0f);
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - agent.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed);
    }
}