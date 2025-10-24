using UnityEngine;
using UnityEngine.AI;

public class AttackState : State
{
    private float attackTimer;

    public AttackState(NpcAI npc, NavMeshAgent agent, Animator animator, Transform player)
        : base(npc, agent, animator, player) { }

    public override void Enter()
    {
        // ��� ����� � ��������� �����, ��������������� � ���������� ������
        agent.isStopped = true;
        agent.ResetPath();
        attackTimer = 0f;

        // �������������� � ������ ����� ������
        LookAtPlayer();

        // ��������� ������� �������� �����
        animator.SetTrigger("Attack");
    }

    public override void Update()
    {
        // --- ������ �������� � ������ ��������� ---
        attackTimer += Time.deltaTime;

        // ����, ���� ������� ����� ����������� (�������)
        if (attackTimer >= npc.attackCooldown)
        {
            // ����� ����� ��� ����� ������, ��� ������ ������.
            // ���������, ��������� �� ����� ��� ��� � ���� ������������.
            float distanceToPlayer = Vector3.Distance(agent.transform.position, player.position);

            if (distanceToPlayer > npc.attackDistance || !npc.HasLineOfSight())
            {
                // ���� ����� ������ ������� ������ ��� �������, ������������ � �������������
                npc.ChangeState(npc.chaseState);
            }
            else
            {
                // ���� ����� ��� ��� �����, ����� ��������� �����.
                // ��� ����� �� ������ "�������������" ������� ��������� �����.
                npc.ChangeState(npc.attackState);
            }
        }
    }

    public override void Exit()
    {
        // ��� ������ �� ����� ��������� ��� ������ ���������� ������ �� �����
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - agent.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        // ���������� Slerp � ������� �������������, ����� ������� ��� ����� ����������
        agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, 1f);
    }
}