using UnityEngine;
using UnityEngine.AI;

public abstract class State
{
    protected NpcAI npc;
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform player;

    /// <summary>
    /// ����������� ��� �������� ���� ����������� ������ � ���������.
    /// </summary>
    public State(NpcAI npc, NavMeshAgent agent, Animator animator, Transform player)
    {
        this.npc = npc;
        this.agent = agent;
        this.animator = animator;
        this.player = player;
    }

    /// <summary>
    /// ���������� ���� ��� ��� ����� � ���������.
    /// �������� ��� ������� ��������, ��������� ������.
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// ���������� ������ ����, ���� ��������� �������. ������ Update().
    /// ����� ����� �������� ������ ���������.
    /// </summary>
    public abstract void Update();

    /// <summary>
    /// ���������� ���� ��� ��� ������ �� ���������.
    /// �������� ��� ������ ��������, ������� ������.
    /// </summary>
    public virtual void Exit() { }
}