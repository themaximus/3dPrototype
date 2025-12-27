using UnityEngine;

public class TrainBogie : MonoBehaviour
{
    [Header("Настройки Пути")]
    public RailPath currentRail;
    public float distanceOnRail;

    [Header("Главные Тележки")]
    public Transform bogieFront;
    public Transform bogieRear;

    [Header("Сцепки")]
    public TrainCoupler frontCoupler;
    public TrainCoupler rearCoupler;

    [Header("Визуал")]
    public float heightOffset = 0.0f;
    public Vector3 rotationOffset;

    public bool isLockedByTipper = false;
    [HideInInspector] public bool isLocomotive = false;

    private float bogieSpacing;
    private Vector3 localBogieFrontPos;
    private Vector3 localBogieRearPos;

    void Awake()
    {
        if (frontCoupler == null) frontCoupler = transform.Find("Coupler_Front")?.GetComponent<TrainCoupler>();
        if (rearCoupler == null) rearCoupler = transform.Find("Coupler_Rear")?.GetComponent<TrainCoupler>();

        if (bogieFront == null || bogieRear == null)
        {
            this.enabled = false;
            return;
        }

        bogieSpacing = Vector3.Distance(bogieFront.position, bogieRear.position);
        localBogieFrontPos = bogieFront.localPosition;
        localBogieRearPos = bogieRear.localPosition;
    }

    void Start()
    {
        if (currentRail != null) UpdatePosition(currentRail, distanceOnRail);
    }

    public void UpdatePosition(RailPath rail, float distance)
    {
        if (bogieFront == null || bogieRear == null) return;
        if (isLockedByTipper) return;

        // --- 1. ПРОВЕРКА НА ДВИЖЕНИЕ НАЗАД (С ВЕТКИ НА РОДИТЕЛЯ) ---
        if (rail == currentRail && distance < 0)
        {
            // Если у текущего пути есть родитель (значит, мы на ветке)
            if (rail.parentPath != null)
            {
                // Точка стыка на родителе
                float junctionPoint = rail.startDistanceOnParent;

                // distance у нас отрицательная (например, -0.5 метра).
                // Значит, мы вылезли на родителя на 0.5 метра назад от стыка.
                float newDistance = junctionPoint + distance;

                // Переключаем путь
                currentRail = rail.parentPath;
                distanceOnRail = newDistance;

                Debug.Log($"[{name}] 🔙 Возврат с ветки на родительский путь: {currentRail.name}");

                // Рекурсивно обновляемся на новом пути
                UpdatePosition(currentRail, distanceOnRail);
                return;
            }
        }
        // -------------------------------------------------------------

        // --- 2. ПРОВЕРКА НА ДВИЖЕНИЕ ВПЕРЕД (С РОДИТЕЛЯ НА ВЕТКУ) ---
        if (rail == currentRail && distance > distanceOnRail)
        {
            foreach (var junction in rail.junctions)
            {
                if (distanceOnRail <= junction.distanceOnRail && distance >= junction.distanceOnRail)
                {
                    if (junction.branchPath != null && junction.branchPath.isSwitchOpen)
                    {
                        Debug.Log($"[{name}] 🔀 Сворачиваем на ветку: {junction.branchPath.name}");

                        currentRail = junction.branchPath;
                        float overshoot = distance - junction.distanceOnRail;
                        distanceOnRail = overshoot;

                        UpdatePosition(currentRail, distanceOnRail);
                        return;
                    }
                }
            }
        }

        currentRail = rail;
        distanceOnRail = distance;

        if (currentRail == null) return;

        if (currentRail.loop)
            distanceOnRail = Mathf.Repeat(distanceOnRail, currentRail.TotalLength);
        else
            distanceOnRail = Mathf.Clamp(distanceOnRail, 0, currentRail.TotalLength);

        // Расчет позиций (как раньше)
        Vector3 posA; Quaternion rotA;
        currentRail.GetPointAtDistance(distanceOnRail, out posA, out rotA);

        float distRear = distanceOnRail - bogieSpacing;
        if (currentRail.loop) distRear = Mathf.Repeat(distRear, currentRail.TotalLength);

        Vector3 posB; Quaternion rotB;
        float solverDist = distRear;
        for (int i = 0; i < 3; i++)
        {
            currentRail.GetPointAtDistance(solverDist, out posB, out rotB);
            float d = Vector3.Distance(posA, posB);
            solverDist -= (bogieSpacing - d);
        }
        currentRail.GetPointAtDistance(solverDist, out posB, out rotB);

        Vector3 wagonDir = (posA - posB).normalized;
        Vector3 wagonUp = (rotA * Vector3.up + rotB * Vector3.up).normalized;

        if (wagonDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(wagonDir, wagonUp) * Quaternion.Euler(rotationOffset);

        Vector3 railCenter = (posA + posB) * 0.5f;
        Vector3 localCenter = (localBogieFrontPos + localBogieRearPos) * 0.5f;
        transform.position = railCenter - (transform.rotation * localCenter);
        transform.position += transform.up * heightOffset;

        bogieFront.position = posA + (transform.up * heightOffset);
        bogieFront.rotation = rotA;

        bogieRear.position = posB + (transform.up * heightOffset);
        bogieRear.rotation = rotB;
    }

    public void ForceUpdatePosition()
    {
        UpdatePosition(currentRail, distanceOnRail);
    }
}