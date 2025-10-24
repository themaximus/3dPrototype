using UnityEngine;
using UnityEngine.AI;

public class IdleState : State
{
    // --- ����� ���� ��� ����������� ---
    private float sightCheckTimer;
    private float sightCheckCooldown = 0.3f; // ��������� ������ �������� 3 ���� � �������

    public IdleState(NpcAI npc, NavMeshAgent agent, Animator animator, Transform player)
        : base(npc, agent, animator, player) { }

    public override void Enter()
    {
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetFloat("Speed", 0f);
        sightCheckTimer = 0f; // ���������� ������ ��� ����� � ���������
    }

    public override void Update()
    {
        // --- ����������� ������ � �������� ---
        sightCheckTimer += Time.deltaTime;

        // ��������� �������� ������ ���� ������ ���������� �������
        if (sightCheckTimer >= sightCheckCooldown)
        {
            sightCheckTimer = 0f; // ���������� ������

            // ���� NPC ����� ������ � ����� �������-����...
            if (npc.IsPlayerInZone() && npc.HasLineOfSight())
            {
                // ...������������� � ��������� �������������
                npc.ChangeState(npc.chaseState);
            }
        }
    }

    public override void Exit()
    {
        // ������ ������ �� �����
    }
}