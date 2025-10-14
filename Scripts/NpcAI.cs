using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NpcAI : MonoBehaviour
{
    // === ПУБЛИЧНЫЕ ПОЛЯ (настраиваются в инспекторе) ===
    [Tooltip("Трансформ игрока, за которым будет следовать NPC")]
    public Transform playerTransform;

    [Tooltip("Дистанция, на которой NPC начнет атаковать")]
    public float attackDistance = 2f;

    [Tooltip("Время между атаками в секундах")]
    public float attackCooldown = 2f;

    [Tooltip("ВАЖНО: точное имя состояния атаки в Animator")]
    public string attackStateName = "HumanArmature|Man_Punch"; // Замените на ваше имя!

    // === ПРИВАТНЫЕ ПОЛЯ ===
    private NavMeshAgent agent;
    private Animator animator;
    private bool isPlayerInZone = false;
    private float timeSinceLastAttack = 0f;
    private const string PLAYER_TAG = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogError("Не удалось найти объект игрока с тегом 'Player'.");
                this.enabled = false;
            }
        }
    }

    void Update()
    {
        // Если игрока нет в зоне видимости, ничего не делаем
        if (!isPlayerInZone || playerTransform == null)
        {
            // Убеждаемся, что NPC стоит на месте
            if (agent.hasPath) agent.ResetPath();
            UpdateAnimations();
            return;
        }

        // Если мы сейчас в процессе атаки, нужно стоять на месте и ничего не делать.
        if (IsAttacking())
        {
            agent.isStopped = true; // Приостанавливаем движение, но не сбрасываем путь
            return;
        }
        else
        {
            agent.isStopped = false; // Возобновляем движение, если не атакуем
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackDistance)
        {
            // --- ЛОГИКА АТАКИ ---
            // Останавливаемся и атакуем, если кулдаун прошел
            agent.ResetPath(); // Сбрасываем путь, чтобы он не ехал по инерции
            LookAtPlayer();
            Attack();
        }
        else
        {
            // --- ЛОГИКА ПРЕСЛЕДОВАНИЯ ---
            // Если игрок дальше дистанции атаки, преследуем его
            agent.SetDestination(playerTransform.position);
        }

        timeSinceLastAttack += Time.deltaTime;
        UpdateAnimations();
    }

    // НОВЫЙ МЕТОД для обновления анимаций
    private void UpdateAnimations()
    {
        // Получаем текущую скорость агента. magnitude - это длина вектора скорости.
        float speed = agent.velocity.magnitude;
        // Передаем скорость в аниматор. 
        // Если скорость > 0, персонаж бежит. Если равна 0 - стоит.
        animator.SetFloat("Speed", speed);
    }

    private void Attack()
    {
        if (timeSinceLastAttack >= attackCooldown)
        {
            animator.SetTrigger("Attack");
            timeSinceLastAttack = 0f;
        }
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed);
    }

    // ПРОВЕРЯЕМ, проигрывается ли сейчас анимация атаки
    private bool IsAttacking()
    {
        // GetCurrentAnimatorStateInfo(0) - получаем инфо о текущем состоянии в первом слое аниматора
        // .IsName() - проверяем, совпадает ли имя состояния с нашим
        return animator.GetCurrentAnimatorStateInfo(0).IsName(attackStateName);
    }

    // Триггеры входа/выхода из зоны агрессии (остаются без изменений)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG)) isPlayerInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG)) isPlayerInZone = false;
    }
}