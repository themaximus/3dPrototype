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
        Debug.Log($"{gameObject.name} ����������� �������� ������");

        // --- ��������� ����� ---
        // �������� ���������������� ����� ������ � NpcAI
        if (npcAI != null)
        {
            npcAI.InitiateDeath();
        }

        // ��������� ���������, ����� ����� ��� ������ ������ ������� ����
        if (npcCollider != null)
        {
            npcCollider.enabled = false;
        }

        // NavMeshAgent ����������� ������ InitiateDeath, ��� ��� ����� ��� ������� �� �����.

        // ��������� ��������
        animator.SetTrigger("Death");
    }
}