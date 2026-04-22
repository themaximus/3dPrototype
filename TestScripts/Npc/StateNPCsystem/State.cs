using UnityEngine;
using UnityEngine.AI;

public abstract class State
{
    protected NpcAI npc;
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform player;

    /// <summary>
    /// Конструктор для передачи всех необходимых ссылок в состояние.
    /// </summary>
    public State(NpcAI npc, NavMeshAgent agent, Animator animator, Transform player)
    {
        this.npc = npc;
        this.agent = agent;
        this.animator = animator;
        this.player = player;
    }

    /// <summary>
    /// Вызывается один раз при входе в состояние.
    /// Идеально для запуска анимаций, установки флагов.
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// Вызывается каждый кадр, пока состояние активно. Аналог Update().
    /// Здесь будет основная логика поведения.
    /// </summary>
    public abstract void Update();

    /// <summary>
    /// Вызывается один раз при выходе из состояния.
    /// Идеально для сброса анимаций, очистки данных.
    /// </summary>
    public virtual void Exit() { }
}