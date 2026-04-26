using UnityEngine;

// Главный скрипт: берет вашу формулу вычисления дуги луча и сглаживания Y
public class MetalDetectorIK : MonoBehaviour
{
    [Header("Ссылки на модули")]
    public MetalDetectorSway swayModule;
    public MetalDetectorScanner scannerModule;

    [Header("Основные объекты")]
    public Transform coilTransform;
    public Transform handleTransform;
    public Transform raycastOrigin;

    [Header("Настройки Катушки (Ведущая)")]
    public Vector3 modelUpAxis = Vector3.up;
    public float coilRotationSpeed = 15f;
    public float coilPositionSpeed = 15f;
    public float coilGroundOffset = 0.05f;
    public float maxBackwardLimit = 0.1f;

    // Кэш (как в оригинале)
    private Vector3 defaultCoilLocalPos;
    private Quaternion defaultCoilLocalRot;
    private Vector3 handleLocalOffsetFromCoil;
    private Quaternion defaultHandleLocalRot;
    private Vector3 defaultRaycastLocalPos;

    private bool isInitialized = false;
    private float smoothedLocalY;
    private Vector3 smoothedNormal = Vector3.up;

    void Start()
    {
        if (swayModule == null) swayModule = GetComponent<MetalDetectorSway>();
        if (scannerModule == null) scannerModule = GetComponent<MetalDetectorScanner>();

        if (swayModule != null) swayModule.Initialize();

        if (coilTransform == null || handleTransform == null || raycastOrigin == null || swayModule == null || swayModule.swayPivot == null)
        {
            Debug.LogError("[MetalDetectorIK] Не назначены объекты!");
            enabled = false;
            return;
        }

        defaultCoilLocalPos = coilTransform.localPosition;
        defaultCoilLocalRot = coilTransform.localRotation;
        handleLocalOffsetFromCoil = handleTransform.localPosition - coilTransform.localPosition;
        defaultHandleLocalRot = handleTransform.localRotation;

        defaultRaycastLocalPos = swayModule.swayPivot.InverseTransformPoint(raycastOrigin.position);

        smoothedLocalY = defaultCoilLocalPos.y;
        isInitialized = true;
    }

    void LateUpdate()
    {
        if (!isInitialized) return;

        // 1. Считаем инерцию
        swayModule.CalculateSway();

        Quaternion lagRotation = swayModule.LagRotation;
        Quaternion coilLagRotation = swayModule.CoilLagRotation;
        Transform swayPivot = swayModule.swayPivot;

        // 2. ВЫЧИСЛЕНИЕ ДУГИ И ЛУЧА (ВОТ ОНО, ВОЗВРАЩЕНО!)
        Vector3 worldDefaultRayPos = swayPivot.TransformPoint(defaultRaycastLocalPos);
        Vector3 offsetFromPivot = worldDefaultRayPos - swayPivot.position;
        Vector3 activeRayOrigin = swayPivot.position + lagRotation * offsetFromPivot;

        // 3. СКАНИРОВАНИЕ ЗЕМЛИ
        scannerModule.ScanGround(activeRayOrigin, swayPivot.forward);

        // 4. ВЫЧИСЛЕНИЕ ПОЗИЦИИ КАТУШКИ (ТОЧНО КАК В ВАШЕМ ОРИГИНАЛЕ)
        Vector3 rawLocalPos;
        Vector3 targetNormal = Vector3.up;

        if (scannerModule.HasGround)
        {
            Vector3 worldGroundPos = scannerModule.GroundHitPosition + scannerModule.GroundHitNormal * coilGroundOffset;
            rawLocalPos = coilTransform.parent.InverseTransformPoint(worldGroundPos);
            targetNormal = scannerModule.GroundHitNormal;
        }
        else
        {
            Vector3 worldDefaultCoilPos = coilTransform.parent.TransformPoint(defaultCoilLocalPos);
            Vector3 laggedWorldCoilPos = swayPivot.position + coilLagRotation * (worldDefaultCoilPos - swayPivot.position);
            rawLocalPos = coilTransform.parent.InverseTransformPoint(laggedWorldCoilPos);
            targetNormal = Vector3.up;
        }

        // 5. СГЛАЖИВАНИЕ ПОЗИЦИИ И НОРМАЛИ (ТОЧНО КАК В ВАШЕМ ОРИГИНАЛЕ)
        smoothedLocalY = Mathf.Lerp(smoothedLocalY, rawLocalPos.y, Time.deltaTime * coilPositionSpeed);
        smoothedNormal = Vector3.Slerp(smoothedNormal, targetNormal, Time.deltaTime * coilRotationSpeed);

        Vector3 finalLocalPos = new Vector3(rawLocalPos.x, smoothedLocalY, rawLocalPos.z);
        finalLocalPos.z = Mathf.Max(finalLocalPos.z, defaultCoilLocalPos.z - maxBackwardLimit);

        // 6. ПРИМЕНЕНИЕ ПОЗИЦИЙ (ТОЧНО КАК В ВАШЕМ ОРИГИНАЛЕ)
        coilTransform.localPosition = finalLocalPos;

        Vector3 worldHandleOffset = handleTransform.parent.TransformVector(handleLocalOffsetFromCoil);
        Vector3 laggedWorldOffset = lagRotation * worldHandleOffset;
        handleTransform.localPosition = finalLocalPos + handleTransform.parent.InverseTransformVector(laggedWorldOffset);

        // 7. ВРАЩЕНИЕ (ТОЧНО КАК В ВАШЕМ ОРИГИНАЛЕ)
        Quaternion baseCoilWorldRot = coilTransform.parent.rotation * defaultCoilLocalRot;
        Quaternion laggedCoilWorldRot = coilLagRotation * baseCoilWorldRot;
        Vector3 currentUp = laggedCoilWorldRot * modelUpAxis;
        Quaternion groundAlign = Quaternion.FromToRotation(currentUp, smoothedNormal);

        coilTransform.rotation = groundAlign * laggedCoilWorldRot;

        Quaternion baseHandleWorldRot = handleTransform.parent.rotation * defaultHandleLocalRot;
        handleTransform.rotation = lagRotation * baseHandleWorldRot;

        // 8. Обновление радара
        if (scannerModule.IsActive)
        {
            scannerModule.ScanLoot(coilTransform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (swayModule == null || swayModule.swayPivot == null || raycastOrigin == null) return;

        Transform swayPivot = swayModule.swayPivot;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(swayPivot.position, 0.05f);

        float previewLag = Application.isPlaying ? swayModule.LagAngle : 0f;
        Quaternion previewRot = Quaternion.Euler(0, previewLag, 0);

        Vector3 defaultRaycastLocalPosGizmo = swayPivot.InverseTransformPoint(raycastOrigin.position);
        Vector3 worldDefaultRayPos = swayPivot.TransformPoint(defaultRaycastLocalPosGizmo);
        Vector3 offsetFromPivot = worldDefaultRayPos - swayPivot.position;
        Vector3 activeOrigin = swayPivot.position + previewRot * offsetFromPivot;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(swayPivot.position, activeOrigin);

        if (scannerModule != null)
        {
            Gizmos.DrawLine(activeOrigin, activeOrigin + Vector3.down * scannerModule.rayDistance);

            if (coilTransform != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.DrawWireSphere(coilTransform.position, scannerModule.maxLootDetectDistance);
            }
        }
    }
}