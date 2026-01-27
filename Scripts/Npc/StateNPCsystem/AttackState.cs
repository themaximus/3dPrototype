using UnityEngine;
using UnityEngine.AI;

public class AttackState : State
{
    private float attackTimer;

    public AttackState(NpcAI npc, NavMeshAgent agent, Animator animator, Transform player)
        : base(npc, agent, animator, player) { }

    public override void Enter()
    {
        // При входе в состояние атаки, останавливаемся и сбрасываем таймер
        agent.isStopped = true;
        agent.ResetPath();
        attackTimer = 0f;

        // Поворачиваемся к игроку перед атакой
        LookAtPlayer();

        // Запускаем триггер анимации атаки
        animator.SetTrigger("Attack");
    }

    public override void Update()
    {
        // --- Логика перехода в другие состояния ---
        attackTimer += Time.deltaTime;

        // --- ИЗМЕНЕНИЕ ЗДЕСЬ (1) ---
        // Ждем, пока пройдет время перезарядки (кулдаун)
        // Было: if (attackTimer >= npc.attackCooldown)
        if (attackTimer >= npc.stats.attackCooldown) // Стало: используем статы
        {
            // После атаки нам нужно решить, что делать дальше.
            // Проверяем, находится ли игрок все еще в зоне досягаемости.
            float distanceToPlayer = Vector3.Distance(agent.transform.position, player.position);

            // --- ИЗМЕНЕНИЕ ЗДЕСЬ (2) ---
            // Было: if (distanceToPlayer > npc.attackDistance || !npc.HasLineOfSight())
            if (distanceToPlayer > npc.stats.attackRange || !npc.HasLineOfSight()) // Стало: используем статы
            {
                // Если игрок отошел слишком далеко или скрылся, возвращаемся к преследованию
                npc.ChangeState(npc.chaseState);
            }
            else
            {
                // Если игрок все еще рядом, можно атаковать снова.
                // Для этого мы просто "перезапускаем" текущее состояние атаки.
                npc.ChangeState(npc.attackState);
            }
        }
    }

    public override void Exit()
    {
        // При выходе из этого состояния нам ничего особенного делать не нужно
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - agent.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        // Используем Slerp с большим коэффициентом, чтобы поворот был почти мгновенным
        agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, 1f);
    }
}