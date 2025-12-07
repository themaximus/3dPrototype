using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(TrainBogie))]
public class LocomotiveController : MonoBehaviour
{
    [Header("Управление")]
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    public float brakeForce = 10f;

    [Tooltip("Если 0, сцепки будут касаться точка в точку")]
    public float couplingGap = 0.0f;

    [Header("Инфо")]
    public float currentSpeed = 0f;
    [Range(-1, 1)] public int throttleInput = 0;

    private TrainBogie myBogie;

    // Защита от бесконечного цикла (StackOverflow)
    private HashSet<TrainBogie> processedBogies = new HashSet<TrainBogie>();

    void Awake()
    {
        myBogie = GetComponent<TrainBogie>();
        myBogie.isLocomotive = true;
    }

    void Update()
    {
        // 1. Физика
        if (throttleInput != 0)
            currentSpeed += throttleInput * acceleration * Time.deltaTime;
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeForce * Time.deltaTime);

        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // 2. Движение
        if (myBogie.currentRail != null)
        {
            // Очищаем список перед кадром
            processedBogies.Clear();
            processedBogies.Add(myBogie);

            float moveDist = currentSpeed * Time.deltaTime;
            myBogie.UpdatePosition(myBogie.currentRail, myBogie.distanceOnRail + moveDist);

            // 3. Тянем соседей (если есть)
            if (myBogie.rearCoupler != null && myBogie.rearCoupler.IsCoupled)
                PullNeighbor(myBogie, myBogie.rearCoupler);

            if (myBogie.frontCoupler != null && myBogie.frontCoupler.IsCoupled)
                PullNeighbor(myBogie, myBogie.frontCoupler);
        }
    }

    void PullNeighbor(TrainBogie hostBogie, TrainCoupler hostCoupler)
    {
        TrainCoupler neighborCoupler = hostCoupler.connectedCoupler;
        if (neighborCoupler == null) return;

        TrainBogie neighborBogie = neighborCoupler.myBogie;

        // ЗАЩИТА: Если мы уже двигали этот вагон в этом кадре — выходим
        if (processedBogies.Contains(neighborBogie)) return;
        processedBogies.Add(neighborBogie);

        // --- МАТЕМАТИКА 1-в-1 ---
        // 1. Где сцепка Хоста на рельсе?
        float hostCouplerDist = hostBogie.distanceOnRail + hostBogie.GetCouplerOffset(hostCoupler);

        // 2. Куда смещать соседа?
        float direction = (hostCoupler.type == TrainCoupler.CouplerType.Front) ? 1f : -1f;
        float targetContactPoint = hostCouplerDist + (couplingGap * direction);

        // 3. Где должен быть центр Соседа?
        float neighborCenterDist = targetContactPoint - neighborBogie.GetCouplerOffset(neighborCoupler);

        neighborBogie.UpdatePosition(hostBogie.currentRail, neighborCenterDist);

        // 4. Рекурсия (идем дальше по цепочке)
        TrainCoupler nextCoupler = (neighborCoupler == neighborBogie.frontCoupler) ? neighborBogie.rearCoupler : neighborBogie.frontCoupler;
        if (nextCoupler != null && nextCoupler.IsCoupled)
        {
            PullNeighbor(neighborBogie, nextCoupler);
        }
    }

    public void EmergencyStop()
    {
        currentSpeed = 0; throttleInput = 0;
    }
}