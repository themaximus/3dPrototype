using UnityEngine;
using UnityEngine.AI;

public class IdleState : State
{
    // --- НОВЫЕ ПОЛЯ ДЛЯ ОПТИМИЗАЦИИ ---
    private float sightCheckTimer;
    private float sightCheckCooldown = 0.3f; // Проверяем зрение примерно 3 раза в секунду

    public IdleState(NpcAI npc, NavMeshAgent agent, Animator animator, Transform player)
        : base(npc, agent, animator, player) { }

    public override void Enter()
    {
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetFloat("Speed", 0f);
        sightCheckTimer = 0f; // Сбрасываем таймер при входе в состояние
    }

    public override void Update()
    {
        // --- ОБНОВЛЕННАЯ ЛОГИКА С ТАЙМЕРОМ ---
        sightCheckTimer += Time.deltaTime;

        // Выполняем проверку только если прошло достаточно времени
        if (sightCheckTimer >= sightCheckCooldown)
        {
            sightCheckTimer = 0f; // Сбрасываем таймер

            // Если NPC видит игрока в своей триггер-зоне...
            if (npc.IsPlayerInZone() && npc.HasLineOfSight())
            {
                // ...переключаемся в состояние преследования
                npc.ChangeState(npc.chaseState);
            }
        }
    }

    public override void Exit()
    {
        // Ничего делать не нужно
    }
}