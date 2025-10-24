using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NpcAI : MonoBehaviour
{
    [Header("Core Components")]
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Transform playerTransform;

    [Header("State Machine")]
    private State currentState;
    [HideInInspector] public IdleState idleState;
    [HideInInspector] public ChaseState chaseState;
    [HideInInspector] public AttackState attackState;

    [Header("AI Settings")]
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public Transform viewPoint;
    public LayerMask visionBlockers; // �����: ���������, ��� ���� ������ ����� �� ������!

    private bool isPlayerInZone = false;
    private bool isDead = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) playerTransform = playerObject.transform;
        else Debug.LogError("����������� ������: ����� � ����� 'Player' �� ������!");

        idleState = new IdleState(this, agent, animator, playerTransform);
        chaseState = new ChaseState(this, agent, animator, playerTransform);
        attackState = new AttackState(this, agent, animator, playerTransform);

        if (viewPoint == null) viewPoint = transform;
    }

    private void Start()
    {
        ChangeState(idleState);
        Debug.Log("NpcAI �������. ��������� ���������: " + currentState.GetType().Name);
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
            Debug.Log("��������� �������� ��: " + currentState.GetType().Name);
        }
    }

    // --- ����������� ����� ---
    public bool HasLineOfSight()
    {
        if (playerTransform == null) return false;

        Vector3 directionToPlayer = (playerTransform.position - viewPoint.position).normalized;
        float distanceToPlayer = Vector3.Distance(viewPoint.position, playerTransform.position);

        // ������� ��� � �������, �� ��� �� �����
        if (Physics.Raycast(viewPoint.position, directionToPlayer, out RaycastHit hit, distanceToPlayer, visionBlockers, QueryTriggerInteraction.Ignore))
        {
            // ���� ��� ����� �� ���-��, ��� �� �������� �������, ������, ��������� �������������
            if (hit.transform != playerTransform)
            {
                Debug.DrawRay(viewPoint.position, directionToPlayer * distanceToPlayer, Color.red);
                Debug.Log("��� ������ ������������ ��������: " + hit.transform.name);
                return false;
            }
        }

        // ���� ��� �� �� ��� �� ����� ��� ����� ������ � ������, ������, �� ��� �����
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
        Debug.Log("����� ����� � �������-����!");
        isPlayerInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (isDead || !other.CompareTag("Player")) return;
        Debug.Log("����� ������� �������-����!");
        isPlayerInZone = false;
    }
}