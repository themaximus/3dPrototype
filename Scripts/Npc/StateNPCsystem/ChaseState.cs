using UnityEngine;
using UnityEngine.AI;

public class ChaseState : State
{
    private float chaseTimer;
    private float pathUpdateRate = 0.2f; // ��� ����� ��������� ����

    public ChaseState(NpcAI npc, NavMeshAgent agent, Animator animator, Transform player)
        : base(npc, agent, animator, player) { }

    public override void Enter()
    {
        // ��� ����� � ��������� �������������, ��������� ������ ���������
        agent.isStopped = false;
        chaseTimer = 0f;
    }

    public override void Update()
    {
        // --- ������ �������� � ������ ��������� ---

        // 1. ���� �������� ������ �� ���� ��� �� ����� �� ����...
        if (!npc.IsPlayerInZone() || !npc.HasLineOfSight())
        {
            // ...������������� � ��������� ��������
            npc.ChangeState(npc.idleState);
            return; // �������, ����� �� ��������� ��������� ������
        }

        // 2. ���� �������� �� ������ � ����� ���������...
        float distanceToPlayer = Vector3.Distance(agent.transform.position, player.position);

        // --- ��������� ����� ---
        // ����: if (distanceToPlayer <= npc.attackDistance)
        if (distanceToPlayer <= npc.stats.attackRange) // �����: ���������� �����
        {
            // ...������������� � ��������� �����
            npc.ChangeState(npc.attackState);
            return; // �������
        }

        // --- �������� ������ ��������� ---
        // ��������� ���� � ������ �� ������ ����, � � �������� ��������
        chaseTimer += Time.deltaTime;
        if (chaseTimer >= pathUpdateRate)
        {
            agent.SetDestination(player.position);
            chaseTimer = 0f;
        }

        // ��������� �������� ����
        animator.SetFloat("Speed", agent.velocity.magnitude);

        // ������ �������������� � ������� ������
        LookAtPlayer();
    }

    public override void Exit()
    {
        // ��� ������ �� ����� ��������� ������������� ������
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