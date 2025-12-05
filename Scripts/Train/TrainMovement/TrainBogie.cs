using UnityEngine;

public class TrainBogie : MonoBehaviour
{
    [Header("Настройки Пути")]
    public RailPath currentRail;
    public float distanceOnRail;
    public float rearRailDist { get; private set; }

    [Header("Сцепки")]
    public TrainCoupler frontCoupler;
    public TrainCoupler rearCoupler;
    [Tooltip("Технический зазор (0.01 = 1см)")]
    public float couplingGap = 0.01f;

    [Header("Геометрия")]
    public Transform frontAnchor;
    public Transform rearAnchor;
    public Transform heightAnchor;

    [Header("Настройка Высоты")]
    public float heightOffset = 0.0f;

    [Header("Визуал")]
    public Vector3 rotationOffset;

    [Header("Состояние")]
    public bool isLockedByTipper = false;
    [Tooltip("Ставится автоматически контроллером локомотива")]
    public bool isLocomotive = false;

    private float bogieLength;
    private Vector3 centerOffset;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        if (frontCoupler == null || rearCoupler == null)
        {
            var couplers = GetComponentsInChildren<TrainCoupler>();
            foreach (var c in couplers)
            {
                if (c.type == TrainCoupler.CouplerType.Front) frontCoupler = c;
                else if (c.type == TrainCoupler.CouplerType.Rear) rearCoupler = c;
            }
        }

        if (frontAnchor != null && rearAnchor != null)
        {
            bogieLength = Vector3.Distance(frontAnchor.position, rearAnchor.position);
            Vector3 localFront = transform.InverseTransformPoint(frontAnchor.position);
            Vector3 localRear = transform.InverseTransformPoint(rearAnchor.position);
            centerOffset = (localFront + localRear) * 0.5f;
            centerOffset.y = 0;
        }
        else
        {
            Debug.LogError($"[TrainBogie] ❌ {name}: Нет якорей!");
            this.enabled = false;
        }

        UpdatePosition();
    }

    void LateUpdate()
    {
        if (isLockedByTipper) return;

        // ГЛАВНОЕ ИЗМЕНЕНИЕ:
        // Движение инициирует ТОЛЬКО Локомотив.
        // Он двигает себя, а потом запускает "волну", которая двигает все прицепленные вагоны.
        if (isLocomotive)
        {
            UpdatePosition(); // Обновляем свою позицию (LocoController уже поменял distanceOnRail)
            MoveChain(this);  // Тянем всех, кто прицеплен
        }
        else
        {
            // Если я вагон — я просто обновляю визуал на текущем месте.
            // Мою distanceOnRail поменяет Локомотив (или сосед) через метод MoveByNeighbor.
            UpdatePosition();
        }
    }

    // --- СИСТЕМА АКТИВНОЙ ТЯГИ ---

    // Вызывается тем, кто тянет (sourceBogie), чтобы сдвинуть нас
    public void MoveByNeighbor(TrainCoupler myCoupler, float neighborAnchorPos)
    {
        TrainCoupler partner = myCoupler.connectedCoupler;
        if (partner == null) return;

        // Берем рельс партнера
        if (partner.myBogie.currentRail != null) currentRail = partner.myBogie.currentRail;

        // Рассчитываем, где должна быть НАША сцепка
        // Логика: ПозицияСоседа + (Направление * Зазор)
        // Если сосед Front -> он толкает нас "вперед" (+gap)
        // Если сосед Rear -> он тянет нас "назад" (-gap)
        float direction = (partner.type == TrainCoupler.CouplerType.Front) ? 1f : -1f;
        float targetAnchorPos = neighborAnchorPos + (couplingGap * direction);

        // Применяем к себе
        if (myCoupler.type == TrainCoupler.CouplerType.Front)
        {
            // Нас тянут за перед -> ставим перед в точку
            distanceOnRail = targetAnchorPos;
        }
        else
        {
            // Нас тянут за зад -> перед = зад + длина
            distanceOnRail = targetAnchorPos + bogieLength;
        }

        // Обновляем свою позицию
        UpdatePosition();

        // И передаем эстафету дальше (тянем своих соседей с другой стороны)
        MoveChain(partner.myBogie);
    }

    // Метод запускает волну движения на соседей
    public void MoveChain(TrainBogie caller)
    {
        // Проверяем переднюю сцепку
        if (frontCoupler != null && frontCoupler.IsCoupled)
        {
            // Если там не тот, кто нас только что дернул (чтобы не зациклить)
            if (frontCoupler.connectedCoupler.myBogie != caller)
            {
                // Передаем нашу позицию ПЕРЕДНЕГО якоря
                frontCoupler.connectedCoupler.myBogie.MoveByNeighbor(frontCoupler.connectedCoupler, distanceOnRail);
            }
        }

        // Проверяем заднюю сцепку
        if (rearCoupler != null && rearCoupler.IsCoupled)
        {
            if (rearCoupler.connectedCoupler.myBogie != caller)
            {
                // Передаем нашу позицию ЗАДНЕГО якоря
                rearCoupler.connectedCoupler.myBogie.MoveByNeighbor(rearCoupler.connectedCoupler, rearRailDist);
            }
        }
    }

    // --- ПОЗИЦИОНИРОВАНИЕ (как и было) ---
    public void UpdatePosition()
    {
        if (isLockedByTipper) return;
        if (currentRail == null || bogieLength <= 0) return;

        if (currentRail.loop) distanceOnRail = Mathf.Repeat(distanceOnRail, currentRail.TotalLength);

        // 1. Точки
        Vector3 posFront; Quaternion rotFront;
        currentRail.GetPointAtDistance(distanceOnRail, out posFront, out rotFront);

        float sampleDist = distanceOnRail - bogieLength;
        if (currentRail.loop) sampleDist = Mathf.Repeat(sampleDist, currentRail.TotalLength);

        Vector3 posRear; Quaternion rotRear;

        // Solver
        float currentDist = sampleDist;
        for (int i = 0; i < 3; i++)
        {
            currentRail.GetPointAtDistance(currentDist, out posRear, out rotRear);
            float d = Vector3.Distance(posFront, posRear);
            float diff = bogieLength - d;
            currentDist -= diff;
        }
        currentRail.GetPointAtDistance(currentDist, out posRear, out rotRear);
        rearRailDist = currentDist;

        // 2. Поворот
        Vector3 lookDir = (posFront - posRear).normalized;
        Vector3 avgUp = (rotFront * Vector3.up + rotRear * Vector3.up).normalized;

        Quaternion targetRot = transform.rotation;
        if (lookDir != Vector3.zero)
        {
            targetRot = Quaternion.LookRotation(lookDir, avgUp) * Quaternion.Euler(rotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 20f);
        }

        // 3. Позиция
        Vector3 worldMiddle = (posFront + posRear) * 0.5f;
        Vector3 finalOffset = centerOffset;
        if (heightAnchor != null) finalOffset.y = heightAnchor.localPosition.y;

        Vector3 basePos = worldMiddle - (targetRot * finalOffset);
        transform.position = basePos + (targetRot * Vector3.up * heightOffset);
    }

    public void ForceUpdatePosition()
    {
        // При сцепке вызываем полную волну, чтобы все вагоны выстроились
        MoveChain(null);
        UpdatePosition();
    }

    public void TriggerEmergencyStop()
    {
        if (isLocomotive) GetComponent<LocomotiveController>()?.EmergencyStop();
    }
}