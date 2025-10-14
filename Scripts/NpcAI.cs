using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NpcAI : MonoBehaviour
{
    // === ��������� ���� (������������� � ����������) ===
    [Tooltip("��������� ������, �� ������� ����� ��������� NPC")]
    public Transform playerTransform;

    [Tooltip("���������, �� ������� NPC ������ ���������")]
    public float attackDistance = 2f;

    [Tooltip("����� ����� ������� � ��������")]
    public float attackCooldown = 2f;

    [Tooltip("�����: ������ ��� ��������� ����� � Animator")]
    public string attackStateName = "HumanArmature|Man_Punch"; // �������� �� ���� ���!

    // === ��������� ���� ===
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
                Debug.LogError("�� ������� ����� ������ ������ � ����� 'Player'.");
                this.enabled = false;
            }
        }
    }

    void Update()
    {
        // ���� ������ ��� � ���� ���������, ������ �� ������
        if (!isPlayerInZone || playerTransform == null)
        {
            // ����������, ��� NPC ����� �� �����
            if (agent.hasPath) agent.ResetPath();
            UpdateAnimations();
            return;
        }

        // ���� �� ������ � �������� �����, ����� ������ �� ����� � ������ �� ������.
        if (IsAttacking())
        {
            agent.isStopped = true; // ���������������� ��������, �� �� ���������� ����
            return;
        }
        else
        {
            agent.isStopped = false; // ������������ ��������, ���� �� �������
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackDistance)
        {
            // --- ������ ����� ---
            // ��������������� � �������, ���� ������� ������
            agent.ResetPath(); // ���������� ����, ����� �� �� ���� �� �������
            LookAtPlayer();
            Attack();
        }
        else
        {
            // --- ������ ������������� ---
            // ���� ����� ������ ��������� �����, ���������� ���
            agent.SetDestination(playerTransform.position);
        }

        timeSinceLastAttack += Time.deltaTime;
        UpdateAnimations();
    }

    // ����� ����� ��� ���������� ��������
    private void UpdateAnimations()
    {
        // �������� ������� �������� ������. magnitude - ��� ����� ������� ��������.
        float speed = agent.velocity.magnitude;
        // �������� �������� � ��������. 
        // ���� �������� > 0, �������� �����. ���� ����� 0 - �����.
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

    // ���������, ������������� �� ������ �������� �����
    private bool IsAttacking()
    {
        // GetCurrentAnimatorStateInfo(0) - �������� ���� � ������� ��������� � ������ ���� ���������
        // .IsName() - ���������, ��������� �� ��� ��������� � �����
        return animator.GetCurrentAnimatorStateInfo(0).IsName(attackStateName);
    }

    // �������� �����/������ �� ���� �������� (�������� ��� ���������)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG)) isPlayerInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG)) isPlayerInZone = false;
    }
}