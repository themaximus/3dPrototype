using UnityEngine;

public class TrainBogie : MonoBehaviour
{
    [Header("Настройки Пути")]
    public RailPath currentRail;
    [Tooltip("Дистанция ПЕРЕДНЕЙ тележки от начала пути")]
    public float distanceOnRail;

    [Header("Главные Тележки (Якоря)")]
    [Tooltip("Модель передних колес")]
    public Transform bogieFront;
    [Tooltip("Модель задних колес")]
    public Transform bogieRear;

    [Header("Сцепки")]
    public TrainCoupler frontCoupler;
    public TrainCoupler rearCoupler;

    [Header("Визуал")]
    public float heightOffset = 0.0f;
    public Vector3 rotationOffset;

    // Технические переменные
    public bool isLockedByTipper = false;
    [HideInInspector] public bool isLocomotive = false;

    private float bogieSpacing;
    private Vector3 localBogieFrontPos;
    private Vector3 localBogieRearPos; // <-- Добавили для расчета центра

    void Awake()
    {
        // 1. Авто-поиск сцепок
        if (frontCoupler == null) frontCoupler = transform.Find("Coupler_Front")?.GetComponent<TrainCoupler>();
        if (rearCoupler == null) rearCoupler = transform.Find("Coupler_Rear")?.GetComponent<TrainCoupler>();

        // 2. Проверка тележек
        if (bogieFront == null || bogieRear == null)
        {
            Debug.LogError($"[TrainBogie] ❌ {name}: Не назначены BogieFront/Rear!");
            this.enabled = false;
            return;
        }

        // 3. Вычисляем геометрию
        bogieSpacing = Vector3.Distance(bogieFront.position, bogieRear.position);

        // Запоминаем локальные позиции обоих колес
        localBogieFrontPos = bogieFront.localPosition;
        localBogieRearPos = bogieRear.localPosition;
    }

    void Start()
    {
        if (currentRail != null)
        {
            UpdatePosition(currentRail, distanceOnRail);
        }
    }

    public void UpdatePosition(RailPath rail, float distance)
    {
        if (bogieFront == null || bogieRear == null) return;
        if (isLockedByTipper) return;

        currentRail = rail;
        distanceOnRail = distance;

        if (currentRail == null) return;

        // Ограничение дистанции
        if (currentRail.loop)
            distanceOnRail = Mathf.Repeat(distanceOnRail, currentRail.TotalLength);
        else
            distanceOnRail = Mathf.Clamp(distanceOnRail, 0, currentRail.TotalLength);

        // --- ШАГ 1: Точка передних колес (A) ---
        Vector3 posA; Quaternion rotA;
        currentRail.GetPointAtDistance(distanceOnRail, out posA, out rotA);

        // --- ШАГ 2: Точка задних колес (B) ---
        float distRear = distanceOnRail - bogieSpacing;
        if (currentRail.loop) distRear = Mathf.Repeat(distRear, currentRail.TotalLength);

        Vector3 posB; Quaternion rotB;

        // Солвер длины
        float solverDist = distRear;
        for (int i = 0; i < 3; i++)
        {
            currentRail.GetPointAtDistance(solverDist, out posB, out rotB);
            float d = Vector3.Distance(posA, posB);
            solverDist -= (bogieSpacing - d);
        }
        currentRail.GetPointAtDistance(solverDist, out posB, out rotB);

        // --- ШАГ 3: Ставим КОРПУС (Центрирование) ---
        Vector3 wagonDir = (posA - posB).normalized;
        Vector3 wagonUp = (rotA * Vector3.up + rotB * Vector3.up).normalized;

        transform.rotation = Quaternion.LookRotation(wagonDir, wagonUp) * Quaternion.Euler(rotationOffset);

        // --- ИСПРАВЛЕНИЕ: Центрируем вагон ---
        // 1. Находим середину между колесами на рельсах (Мировой центр)
        Vector3 railCenter = (posA + posB) * 0.5f;

        // 2. Находим середину между колесами внутри вагона (Локальный центр)
        Vector3 localCenter = (localBogieFrontPos + localBogieRearPos) * 0.5f;

        // 3. Ставим вагон так, чтобы локальный центр совпал с мировым
        transform.position = railCenter - (transform.rotation * localCenter);

        // Применяем высоту
        transform.position += transform.up * heightOffset;

        // --- ШАГ 4: Ставим КОЛЕСА ---
        bogieFront.position = posA + (transform.up * heightOffset);
        bogieFront.rotation = rotA;

        bogieRear.position = posB + (transform.up * heightOffset);
        bogieRear.rotation = rotB;
    }

    public void ForceUpdatePosition()
    {
        UpdatePosition(currentRail, distanceOnRail);
    }

    public float GetCouplerOffset(TrainCoupler coupler)
    {
        if (coupler != null && bogieFront != null && bogieRear != null)
        {
            Vector3 axis = (bogieFront.position - bogieRear.position).normalized;
            Vector3 toCoupler = coupler.transform.position - bogieFront.position;
            return Vector3.Dot(toCoupler, axis);
        }
        return 0;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Align to Rail")]
    public void AutoAlign()
    {
        if (currentRail == null)
        {
            Debug.LogError("Назначьте Current Rail!");
            return;
        }
        float bestDist = 0;
        float minDst = float.MaxValue;
        for (float d = 0; d < currentRail.TotalLength; d += 0.5f)
        {
            Vector3 pos; Quaternion rot;
            currentRail.GetPointAtDistance(d, out pos, out rot);
            if (Vector3.Distance(transform.position, pos) < minDst)
            {
                minDst = Vector3.Distance(transform.position, pos);
                bestDist = d;
            }
        }
        distanceOnRail = bestDist;
        Debug.Log($"Wagon aligned at: {distanceOnRail}");
        Awake();
        UpdatePosition(currentRail, distanceOnRail);
    }
#endif
}