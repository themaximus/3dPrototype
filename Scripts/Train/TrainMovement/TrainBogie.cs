using UnityEngine;

public class TrainBogie : MonoBehaviour
{
    [Header("Настройки Пути")]
    public RailPath currentRail;
    public float distanceOnRail; // Позиция ПЕРЕДНЕГО якоря
    public float rearRailDist { get; private set; } // Позиция ЗАДНЕГО якоря

    [Header("Якоря")]
    public Transform frontAnchor;
    public Transform rearAnchor;

    [Header("Сцепка")]
    public TrainBogie leaderBogie;
    public float couplingGap = 0.05f;

    // --- НОВЫЙ ФЛАГ ---
    [Tooltip("Если True, вагон прицеплен ЗАДОМ к переду лидера (его толкают)")]
    public bool connectedViaRear = false;
    // ------------------

    [Header("Визуал")]
    public Vector3 rotationOffset;

    [HideInInspector] public bool isLocomotive = false;

    private float bogieLength;
    private Vector3 meshOffsetFromFront;

    void Start()
    {
        if (frontAnchor != null && rearAnchor != null)
        {
            bogieLength = Vector3.Distance(frontAnchor.position, rearAnchor.position);
            meshOffsetFromFront = transform.InverseTransformPoint(frontAnchor.position);
        }
        // Инициализация
        rearRailDist = distanceOnRail - bogieLength;
    }

    void Update()
    {
        if (leaderBogie != null)
        {
            if (currentRail != leaderBogie.currentRail) currentRail = leaderBogie.currentRail;

            float targetFrontDist = 0;

            if (!connectedViaRear)
            {
                // СТАНДАРТ (Тянут): Наш ПЕРЕД цепляется к ЗАДУ лидера
                targetFrontDist = leaderBogie.rearRailDist - couplingGap;
            }
            else
            {
                // ТОЛКАЮТ: Наш ЗАД цепляется к ПЕРЕДУ лидера
                // Значит наш Зад должен быть там, где Перед лидера
                // А наш Перед = Наш Зад + Наша Длина
                float myRearPos = leaderBogie.distanceOnRail + couplingGap;
                targetFrontDist = myRearPos + bogieLength;
            }

            MoveBogie(targetFrontDist);
        }
        else if (!isLocomotive)
        {
            UpdatePosition();
        }
    }

    public void MoveBogie(float newFrontDist)
    {
        distanceOnRail = newFrontDist;
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (currentRail == null || bogieLength <= 0) return;

        if (currentRail.loop) distanceOnRail = Mathf.Repeat(distanceOnRail, currentRail.TotalLength);

        // 1. Перед
        Vector3 posFront; Quaternion dummy;
        currentRail.GetPointAtDistance(distanceOnRail, out posFront, out dummy);

        // 2. Зад (Solver)
        float currentArcDist = bogieLength;
        Vector3 posRear = Vector3.zero;

        for (int i = 0; i < 5; i++)
        {
            float sampleDist = distanceOnRail - currentArcDist;
            if (currentRail.loop) sampleDist = Mathf.Repeat(sampleDist, currentRail.TotalLength);

            currentRail.GetPointAtDistance(sampleDist, out posRear, out dummy);
            float distError = bogieLength - Vector3.Distance(posFront, posRear);
            currentArcDist += distError;
        }

        rearRailDist = distanceOnRail - currentArcDist;
        if (currentRail.loop) rearRailDist = Mathf.Repeat(rearRailDist, currentRail.TotalLength);

        // 3. Позиционирование
        Vector3 lookDir = (posFront - posRear).normalized;
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir) * Quaternion.Euler(rotationOffset);
        }

        Vector3 worldOffset = transform.TransformVector(meshOffsetFromFront);
        transform.position = posFront - worldOffset;
    }

    // --- МЕТОД ДЛЯ ОСТАНОВКИ ---
    // Ищет локомотив по цепочке и тормозит его
    public void TriggerEmergencyStop()
    {
        if (isLocomotive)
        {
            GetComponent<LocomotiveController>()?.EmergencyStop();
        }
        else if (leaderBogie != null)
        {
            leaderBogie.TriggerEmergencyStop();
        }
    }
}