using UnityEngine;

// Отвечает за физику луча и радар
[RequireComponent(typeof(AudioSource))]
public class MetalDetectorScanner : MonoBehaviour
{
    [Header("Параметры луча и защиты")]
    public LayerMask groundLayer;
    public float rayDistance = 2.0f;
    [Range(0f, 1f)] public float maxLookUpThreshold = 0.4f;

    [Header("Радар скрытых предметов (Лут)")]
    public LayerMask hiddenLootLayer;
    public float maxLootDetectDistance = 4.0f;
    public AudioClip beepSound;
    public float maxBeepInterval = 1.5f;
    public float minBeepInterval = 0.1f;

    // Публичные данные для IK
    public bool IsActive { get; private set; } = false;
    public bool HasGround { get; private set; }
    public Vector3 GroundHitPosition { get; private set; }
    public Vector3 GroundHitNormal { get; private set; }

    private AudioSource audioSource;
    private float beepTimer = 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // Для связи с инвентарем (ЛКМ)
    public void ToggleDetector()
    {
        IsActive = !IsActive;
    }

    public void ScanGround(Vector3 activeRayOrigin, Vector3 pivotForward)
    {
        bool isLookingTooHigh = pivotForward.y > maxLookUpThreshold;

        // ТОЧНО КАК В ВАШЕМ ОРИГИНАЛЕ:
        if (IsActive && !isLookingTooHigh && Physics.Raycast(activeRayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
        {
            HasGround = true;
            GroundHitPosition = hit.point;
            GroundHitNormal = hit.normal;
        }
        else
        {
            HasGround = false;
            GroundHitNormal = Vector3.up;
        }
    }

    public void ScanLoot(Vector3 coilPosition)
    {
        if (!IsActive || beepSound == null) return;

        Collider[] hits = Physics.OverlapSphere(coilPosition, maxLootDetectDistance, hiddenLootLayer);

        if (hits.Length > 0)
        {
            float closestDist = maxLootDetectDistance;
            foreach (var hit in hits)
            {
                float dist = Vector3.Distance(coilPosition, hit.transform.position);
                if (dist < closestDist) closestDist = dist;
            }

            float distanceRatio = closestDist / maxLootDetectDistance;
            float currentInterval = Mathf.Lerp(minBeepInterval, maxBeepInterval, distanceRatio);

            beepTimer -= Time.deltaTime;
            if (beepTimer <= 0f)
            {
                audioSource.PlayOneShot(beepSound);
                beepTimer = currentInterval;
            }
        }
        else
        {
            beepTimer = maxBeepInterval;
        }
    }
}