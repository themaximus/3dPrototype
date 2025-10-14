using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(StatController))]
[RequireComponent(typeof(Animator))]
public class NpcDeathHandler : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private Collider npcCollider;
    private NpcAI npcAI; // ���� � ���� ���� ������ �������������

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        npcCollider = GetComponent<Collider>();
        npcAI = GetComponent<NpcAI>();

        // ������������� �� ������
        GetComponent<StatController>().OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} ����������� �������� ������");

        // ���������� ����� ��������
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // ��������� AI, ����� �� �������� � �� ��������
        if (npcAI != null)
        {
            npcAI.enabled = false;
        }

        // ��������� ��������� (����� �� ����� ����� ����� ������)
        if (npcCollider != null)
        {
            npcCollider.enabled = false;
        }

        // ��������� ��������
        animator.SetTrigger("Death");
    }
}
