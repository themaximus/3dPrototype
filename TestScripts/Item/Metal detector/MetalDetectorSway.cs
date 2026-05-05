using UnityEngine;

// Отвечает только за считывание камеры и расчет углов заноса + Авто-поиск
public class MetalDetectorSway : MonoBehaviour
{
    [Header("Оси маятника")]
    public Transform swayPivot;
    public Transform handleTip;

    [Header("Истинная инерция (Занос)")]
    public float maxSwayAngle = 25f;
    public float swayRecoverySpeed = 5f;

    [Header("Авто-махание (Сканирование)")]
    [Tooltip("Амплитуда: на сколько градусов катушка уходит влево и вправо")]
    public float sweepAngle = 25f;

    [Tooltip("Скорость: как быстро катушка делает один взмах")]
    public float sweepSpeed = 3f;

    [Tooltip("Плавность: скорость разгона при включении и затухания при выключении")]
    public float sweepSmoothness = 5f;

    [Tooltip("Смещение центра взмаха. Если детектор справа, сделайте отрицательным (напр. -15), чтобы отцентровать дугу на экране.")]
    public float sweepCenterOffset = -15f;

    [Header("Тонкая настройка формы (Синусоиды)")]
    public bool useCustomCurve = false;
    public AnimationCurve sweepCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 4f, 4f),
        new Keyframe(0.25f, 1f, 0f, 0f),
        new Keyframe(0.75f, -1f, 0f, 0f),
        new Keyframe(1f, 0f, 4f, 4f)
    );

    // Данные для IK
    public Quaternion LagRotation { get; private set; } = Quaternion.identity;
    public Quaternion CoilLagRotation { get; private set; } = Quaternion.identity;
    public float LagAngle { get; private set; }

    private float currentYaw;
    private Vector3 defaultHandleTipLocalInPivot;
    private bool isInitialized = false;

    // Внутренняя переменная для плавного смещения
    private float currentSweepOffset = 0f;

    public void Initialize()
    {
        if (swayPivot == null && Camera.main != null)
        {
            swayPivot = Camera.main.transform;
        }

        if (swayPivot != null)
        {
            currentYaw = swayPivot.eulerAngles.y;
            if (handleTip != null)
            {
                defaultHandleTipLocalInPivot = swayPivot.InverseTransformPoint(handleTip.position);
            }
            isInitialized = true;
        }
    }

    public void CalculateSway(bool isScannerActive)
    {
        if (!isInitialized || swayPivot == null) return;

        // --- 1. Обычная инерция от поворота камеры ---
        float targetYaw = swayPivot.eulerAngles.y;
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * swayRecoverySpeed);
        float baseLagAngle = Mathf.Clamp(Mathf.DeltaAngle(targetYaw, currentYaw), -maxSwayAngle, maxSwayAngle);

        // --- 2. Расчет синусоиды для авто-махания ---
        float targetSweep = 0f;
        if (isScannerActive)
        {
            if (useCustomCurve)
            {
                float cycleTime = Mathf.Repeat((Time.time * sweepSpeed) / (2f * Mathf.PI), 1f);
                targetSweep = sweepCurve.Evaluate(cycleTime) * sweepAngle;
            }
            else
            {
                targetSweep = Mathf.Sin(Time.time * sweepSpeed) * sweepAngle;
            }

            // Центрируем дугу на экране
            targetSweep += sweepCenterOffset;
        }

        // Плавный переход (Lerp) защищает от резких рывков при включении/выключении
        currentSweepOffset = Mathf.Lerp(currentSweepOffset, targetSweep, Time.deltaTime * sweepSmoothness);

        // --- 3. Складываем инерцию и махание вместе ---
        LagAngle = baseLagAngle + currentSweepOffset;
        LagRotation = Quaternion.Euler(0f, LagAngle, 0f);

        float coilLagAngle = LagAngle;
        CoilLagRotation = LagRotation;

        // --- 4. Коррекция угла для самой катушки ---
        if (handleTip != null)
        {
            Vector3 neutralTipWorld = swayPivot.TransformPoint(defaultHandleTipLocalInPivot);
            Vector3 laggedTipWorld = swayPivot.position + LagRotation * (neutralTipWorld - swayPivot.position);

            Vector3 pivotToNeutral = neutralTipWorld - swayPivot.position;
            Vector3 pivotToLagged = laggedTipWorld - swayPivot.position;

            pivotToNeutral.y = 0f;
            pivotToLagged.y = 0f;

            if (pivotToNeutral.sqrMagnitude > 0.001f && pivotToLagged.sqrMagnitude > 0.001f)
            {
                coilLagAngle = Vector3.SignedAngle(pivotToNeutral, pivotToLagged, Vector3.up);
                CoilLagRotation = Quaternion.Euler(0f, coilLagAngle, 0f);
            }
        }
    }
}