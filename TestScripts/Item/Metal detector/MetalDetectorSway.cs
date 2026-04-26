using UnityEngine;

// Отвечает только за считывание камеры и расчет углов заноса
public class MetalDetectorSway : MonoBehaviour
{
    [Header("Оси маятника")]
    public Transform swayPivot;
    public Transform handleTip;

    [Header("Истинная инерция (Занос)")]
    public float maxSwayAngle = 25f;
    public float swayRecoverySpeed = 5f;

    // Данные для IK
    public Quaternion LagRotation { get; private set; } = Quaternion.identity;
    public Quaternion CoilLagRotation { get; private set; } = Quaternion.identity;
    public float LagAngle { get; private set; }

    private float currentYaw;
    private Vector3 defaultHandleTipLocalInPivot;
    private bool isInitialized = false;

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

    public void CalculateSway()
    {
        if (!isInitialized || swayPivot == null) return;

        // ТОЧНО КАК В ВАШЕМ ОРИГИНАЛЕ:
        float targetYaw = swayPivot.eulerAngles.y;
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * swayRecoverySpeed);

        LagAngle = Mathf.Clamp(Mathf.DeltaAngle(targetYaw, currentYaw), -maxSwayAngle, maxSwayAngle);
        LagRotation = Quaternion.Euler(0f, LagAngle, 0f);

        float coilLagAngle = LagAngle;
        CoilLagRotation = LagRotation;

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