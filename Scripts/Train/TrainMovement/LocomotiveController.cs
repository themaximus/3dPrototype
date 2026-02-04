using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(TrainBogie))]
[DefaultExecutionOrder(-200)] // Считаем физику до камеры
public class LocomotiveController : MonoBehaviour
{
    [Header("Управление")]
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    public float brakeForce = 10f;

    [Tooltip("Идеальное расстояние между сцепками")]
    public float couplingGap = 0.05f;

    [Header("Физика Столкновений")]
    public float collisionRayDistance = 1.0f;
    public float collisionRayRadius = 0.5f;
    public float bounceFactor = 0.3f;
    public float touchThreshold = 0.05f;

    [Header("Инфо")]
    public float currentSpeed = 0f;
    [Range(-1, 1)] public int throttleInput = 0;

    public event Action<int> OnGearChanged;

    private TrainBogie myBogie;
    // Защита от повторной обработки одного вагона в кадре
    private HashSet<TrainBogie> processedBogies = new HashSet<TrainBogie>();

    void Awake()
    {
        myBogie = GetComponent<TrainBogie>();
        myBogie.isLocomotive = true;
    }

    void Update()
    {
        CheckCollision();
        HandleSpeed();
        MoveTrain();
    }

    void HandleSpeed()
    {
        if (throttleInput != 0)
        {
            currentSpeed += throttleInput * acceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeForce * Time.deltaTime);
        }
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);
    }

    void MoveTrain()
    {
        if (myBogie.currentRail == null) return;
        if (Mathf.Abs(currentSpeed) < 0.001f) return;

        float moveDist = currentSpeed * Time.deltaTime;

        processedBogies.Clear();
        processedBogies.Add(myBogie);

        // 1. Двигаем сам локомотив
        myBogie.MoveAlongRail(moveDist);

        // 2. Тянем вагоны
        // Логика теперь рекурсивная, но правильная
        if (myBogie.rearCoupler != null && myBogie.rearCoupler.IsCoupled)
            PullNeighbor(myBogie, myBogie.rearCoupler, moveDist);

        if (myBogie.frontCoupler != null && myBogie.frontCoupler.IsCoupled)
            PullNeighbor(myBogie, myBogie.frontCoupler, moveDist);
    }

    /// <summary>
    /// Новая логика сцепки: относительное движение + коррекция ошибки
    /// </summary>
    void PullNeighbor(TrainBogie hostBogie, TrainCoupler hostCoupler, float hostMoveDist)
    {
        TrainCoupler neighborCoupler = hostCoupler.connectedCoupler;
        if (neighborCoupler == null) return;

        TrainBogie neighborBogie = neighborCoupler.myBogie;
        if (processedBogies.Contains(neighborBogie)) return;
        processedBogies.Add(neighborBogie);

        // 1. Считаем ошибку дистанции (насколько сцепка растянулась/сжалась)
        float currentGap = Vector3.Distance(hostCoupler.transform.position, neighborCoupler.transform.position);
        float error = currentGap - couplingGap;

        // 2. Рассчитываем коррекцию.
        // Если gap больше нужного -> error > 0 -> надо подтянуть (двинуть больше).
        // Используем коэффициент 0.5 (мягкость), чтобы система не взрывалась от жестких поправок.
        float correction = 0f;
        if (Mathf.Abs(error) > 0.001f)
        {
            // Если вагоны едут друг в друга, error будет отрицательным, correction тоже
            correction = error * 0.8f;
        }

        // 3. Двигаем вагон
        // Вагон проходит тот же путь, что и локомотив + небольшую коррекцию, чтобы сохранить дистанцию.
        // МЫ НЕ ПЕРЕДАЕМ ЕМУ РЕЛЬС ЛОКОМОТИВА. Вагон сам знает, на каком он рельсе.
        neighborBogie.MoveAlongRail(hostMoveDist + correction);

        // 4. Рекурсивно тянем следующий вагон
        TrainCoupler nextCoupler = (neighborCoupler == neighborBogie.frontCoupler) ? neighborBogie.rearCoupler : neighborBogie.frontCoupler;
        if (nextCoupler != null && nextCoupler.IsCoupled)
        {
            // Следующий вагон должен пройти то же расстояние, что и этот
            PullNeighbor(neighborBogie, nextCoupler, hostMoveDist + correction);
        }
    }

    // --- СТАРЫЙ КОД ФИЗИКИ СТОЛКНОВЕНИЙ ---
    void CheckCollision()
    {
        if (Mathf.Abs(currentSpeed) < 0.1f) return;
        bool movingForward = currentSpeed > 0;

        TrainCoupler sensorCoupler = GetExtremeCoupler(movingForward);
        if (sensorCoupler == null || sensorCoupler.IsCoupled) return;

        Vector3 outwardDir = (sensorCoupler.transform.position - sensorCoupler.myBogie.transform.position).normalized;
        float offsetInside = 0.2f;
        Vector3 origin = sensorCoupler.transform.position - (outwardDir * offsetInside);

        RaycastHit hit;
        if (Physics.SphereCast(origin, collisionRayRadius, outwardDir, out hit, collisionRayDistance))
        {
            TrainCoupler hitCoupler = hit.collider.GetComponent<TrainCoupler>();
            if (hitCoupler != null && hitCoupler.myBogie != sensorCoupler.myBogie)
            {
                float realDistance = hit.distance - offsetInside;
                if (realDistance <= touchThreshold) HandleImpact(hitCoupler.myBogie);
            }
        }
    }

    TrainCoupler GetExtremeCoupler(bool movingForward)
    {
        TrainBogie currentBogie = myBogie;
        TrainCoupler currentExitCoupler = movingForward ? myBogie.frontCoupler : myBogie.rearCoupler;
        HashSet<TrainBogie> visited = new HashSet<TrainBogie> { currentBogie };

        while (currentExitCoupler != null && currentExitCoupler.IsCoupled)
        {
            TrainCoupler nextIncomingCoupler = currentExitCoupler.connectedCoupler;
            TrainBogie nextBogie = nextIncomingCoupler.myBogie;
            if (visited.Contains(nextBogie)) break;
            visited.Add(nextBogie);
            currentBogie = nextBogie;
            currentExitCoupler = (nextIncomingCoupler == nextBogie.frontCoupler) ? nextBogie.rearCoupler : nextBogie.frontCoupler;
        }
        return currentExitCoupler;
    }

    void HandleImpact(TrainBogie targetBogie)
    {
        WagonPhysics wagonPhys = targetBogie.GetComponent<WagonPhysics>();
        LocomotiveController otherLoco = targetBogie.GetComponent<LocomotiveController>();

        if (wagonPhys == null && otherLoco == null) { currentSpeed = 0; SetThrottle(0); return; }

        float impactVelocity = currentSpeed * 0.8f;
        if (wagonPhys != null) wagonPhys.ApplyImpulse(impactVelocity);
        else if (otherLoco != null) otherLoco.currentSpeed = impactVelocity;

        if (Mathf.Abs(currentSpeed) > 5f) currentSpeed = -currentSpeed * bounceFactor;
        else currentSpeed = 0;

        SetThrottle(0);
    }

    public void SetThrottle(int value)
    {
        if (throttleInput != value)
        {
            throttleInput = value;
            OnGearChanged?.Invoke(throttleInput);
        }
    }

    private void OnDrawGizmos()
    {
        if (myBogie == null) myBogie = GetComponent<TrainBogie>();
        // Можно добавить визуализацию, если нужно
    }
}