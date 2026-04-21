using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(StatController))] // <-- ДОБАВЛЕНО
public class NpcAI : MonoBehaviour
{
    [Header("Core Components")]
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Transform playerTransform;

    // --- ДОБАВЛЕННЫЕ ПОЛЯ ---
    [HideInInspector] public StatController statController;
    [HideInInspector] public CharacterStats stats; // Прямая ссылка на статы для FSM
    // -------------------------

    [Header("State Machine")]
    private State currentState;
    [HideInInspector] public IdleState idleState;
    [HideInInspector] public ChaseState chaseState;
    [HideInInspector] public AttackState attackState;

    [Header("AI Settings")]
    // public float attackDistance = 2f; // <-- УДАЛЕНО
    // public float attackCooldown = 2f; // <-- УДАЛЕНО
    public Transform viewPoint;
    public LayerMask visionBlockers; // ВАЖНО: Убедитесь, что слой игрока здесь НЕ выбран!

    private bool isPlayerInZone = false;
    private bool isDead = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // --- ДОБАВЛЕННЫЙ БЛОК ---
        // Получаем статы из StatController
        statController = GetComponent<StatController>();
        if (statController != null && statController.characterStats != null)
        {
            stats = statController.characterStats;
        }
        else
        {
            Debug.LogError($"КРИТИЧЕСКАЯ ОШИБКА: {gameObject.name} не имеет StatController или CharacterStats!");
            this.enabled = false;
            return;
        }
        // -------------------------

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) playerTransform = playerObject.transform;
        else Debug.LogError("КРИТИЧЕСКАЯ ОШИБКА: Игрок с тегом 'Player' не найден!");

        idleState = new IdleState(this, agent, animator, playerTransform);
        chaseState = new ChaseState(this, agent, animator, playerTransform);
        attackState = new AttackState(this, agent, animator, playerTransform);

        if (viewPoint == null) viewPoint = transform;
    }

    private void Start()
    {
        ChangeState(idleState);
        Debug.Log("NpcAI запущен. Начальное состояние: " + currentState.GetType().Name);
    }

    private void Update()
    {
        if (isDead || currentState == null) return;
        currentState.Update();
    }

    public void ChangeState(State newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;
        if (currentState != null)
        {
            currentState.Enter();
            Debug.Log("Состояние изменено на: " + currentState.GetType().Name);
        }
    }

    // --- ОБНОВЛЕННЫЙ МЕТОД ---
    public bool HasLineOfSight()
    {
        if (playerTransform == null) return false;

        Vector3 directionToPlayer = (playerTransform.position - viewPoint.position).normalized;
        float distanceToPlayer = Vector3.Distance(viewPoint.position, playerTransform.position);

        // Пускаем луч и смотрим, во что он попал
        if (Physics.Raycast(viewPoint.position, directionToPlayer, out RaycastHit hit, distanceToPlayer, visionBlockers, QueryTriggerInteraction.Ignore))
        {
            // Если луч попал во что-то, что НЕ является игроком, значит, видимость заблокирована
            if (hit.transform != playerTransform)
            {
                Debug.DrawRay(viewPoint.position, directionToPlayer * distanceToPlayer, Color.red);
                Debug.Log("Луч зрения заблокирован объектом: " + hit.transform.name);
                return false;
            }
        }

        // Если луч ни во что не попал ИЛИ попал именно в игрока, значит, мы его видим
        Debug.DrawRay(viewPoint.position, directionToPlayer * distanceToPlayer, Color.green);
        return true;
    }

    public bool IsPlayerInZone()
    {
        return isPlayerInZone;
    }

    public void InitiateDeath()
    {
        if (isDead) return;
        isDead = true;
        this.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead || !other.CompareTag("Player")) return;
        Debug.Log("Игрок ВОШЕЛ в триггер-зону!");
        isPlayerInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (isDead || !other.CompareTag("Player")) return;
        Debug.Log("Игрок ПОКИНУЛ триггер-зону!");
        isPlayerInZone = false;
    }

    // --- ДОБАВЛЕННЫЙ МЕТОД ---
    // Этот метод будет вызываться СОБЫТИЕМ из анимации "Attack"
    public void PerformNpcAttack()
    {
        if (isDead || playerTransform == null) return;

        // Проверяем дистанцию, используя статы из ScriptableObject
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= stats.attackRange)
        {
            StatController playerStats = playerTransform.GetComponent<StatController>();
            if (playerStats != null)
            {
                // Наносим урон, используя статы из ScriptableObject
                Debug.Log($"NPC ({gameObject.name}) наносит {stats.attackDamage} урона игроку!");
                playerStats.TakeDamage(stats.attackDamage);
            }
        }
    }
    // -------------------------
}