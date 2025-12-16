using UnityEngine;
using System; // Нужно для Action
using System.Collections.Generic;

[RequireComponent(typeof(TrainBogie))]
// ГАРАНТИЯ ПЛАВНОСТИ: Скрипт выполняется ДО камеры и физики игрока.
[DefaultExecutionOrder(-200)]
public class LocomotiveController : MonoBehaviour
{
    [Header("Управление")]
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    public float brakeForce = 10f;

    [Tooltip("Расстояние, которое должно поддерживаться между точками сцепки (0 = вплотную)")]
    public float couplingGap = 0.05f;

    [Header("Физика Столкновений")]
    [Tooltip("Максимальная дистанция луча (должна быть больше порога касания)")]
    public float collisionRayDistance = 1.0f; // Увеличил, чтобы заранее видеть, но реагировать только при касании
    [Tooltip("Радиус луча детекции (толщина)")]
    public float collisionRayRadius = 0.5f;
    [Tooltip("Коэффициент отскока (если удар сильный)")]
    public float bounceFactor = 0.3f;
    [Tooltip("Дистанция, при которой считается, что вагоны коснулись (в метрах)")]
    public float touchThreshold = 0.05f;

    [Header("Инфо")]
    public float currentSpeed = 0f;
    [Range(-1, 1)] public int throttleInput = 0;

    // --- СОБЫТИЕ ---
    // На это событие подпишется пульт. Передает int (новую передачу).
    public event Action<int> OnGearChanged;

    private TrainBogie myBogie;
    private HashSet<TrainBogie> processedBogies = new HashSet<TrainBogie>();

    void Awake()
    {
        myBogie = GetComponent<TrainBogie>();
        myBogie.isLocomotive = true;
    }

    void Update()
    {
        // 0. ПРОВЕРКА СТОЛКНОВЕНИЙ
        CheckCollision();

        // 1. Физика скорости
        if (throttleInput != 0)
            currentSpeed += throttleInput * acceleration * Time.deltaTime;
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeForce * Time.deltaTime);

        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // 2. Движение ЛОКОМОТИВА
        if (myBogie.currentRail != null)
        {
            processedBogies.Clear();
            processedBogies.Add(myBogie);

            float moveDist = currentSpeed * Time.deltaTime;
            myBogie.UpdatePosition(myBogie.currentRail, myBogie.distanceOnRail + moveDist);

            // 3. Жесткая привязка соседей
            if (myBogie.rearCoupler != null && myBogie.rearCoupler.IsCoupled)
                PullNeighbor(myBogie, myBogie.rearCoupler, moveDist);

            if (myBogie.frontCoupler != null && myBogie.frontCoupler.IsCoupled)
                PullNeighbor(myBogie, myBogie.frontCoupler, moveDist);
        }
    }

    void CheckCollision()
    {
        if (Mathf.Abs(currentSpeed) < 0.1f) return;

        bool movingForward = currentSpeed > 0;

        // Ищем крайнюю сцепку ВСЕГО состава в направлении движения
        TrainCoupler sensorCoupler = GetExtremeCoupler(movingForward);

        if (sensorCoupler == null) return;
        if (sensorCoupler.IsCoupled) return;

        // Рассчитываем вектор "Наружу" от вагона
        Vector3 outwardDir = (sensorCoupler.transform.position - sensorCoupler.myBogie.transform.position).normalized;

        // Смещаем начало луча немного внутрь вагона (offset), чтобы гарантированно поймать касание в упор
        float offsetInside = 0.2f;
        Vector3 origin = sensorCoupler.transform.position - (outwardDir * offsetInside);

        RaycastHit hit;

        // ВИЗУАЛИЗАЦИЯ (Runtime): Рисуем луч зеленым, если попали, и красным, если нет
        if (Physics.SphereCast(origin, collisionRayRadius, outwardDir, out hit, collisionRayDistance))
        {
            Debug.DrawLine(origin, hit.point, Color.green);

            TrainCoupler hitCoupler = hit.collider.GetComponent<TrainCoupler>();

            if (hitCoupler != null && hitCoupler.myBogie != sensorCoupler.myBogie)
            {
                // ВАЖНО: Вычисляем чистую дистанцию между сцепками
                // hit.distance - это расстояние от origin. Вычитаем offsetInside, чтобы получить расстояние от края сцепки.
                float realDistance = hit.distance - offsetInside;

                // Если дистанция <= порога касания (почти 0), тогда толкаем
                if (realDistance <= touchThreshold)
                {
                    HandleImpact(hitCoupler.myBogie);
                }
            }
        }
        else
        {
            Debug.DrawRay(origin, outwardDir * collisionRayDistance, Color.red);
        }
    }

    /// <summary>
    /// Проходит по цепочке вагонов и находит крайний в заданном направлении.
    /// </summary>
    TrainCoupler GetExtremeCoupler(bool movingForward)
    {
        // Начинаем с локомотива
        TrainBogie currentBogie = myBogie;
        // Если едем вперед - выходим через Front, если назад - через Rear
        TrainCoupler currentExitCoupler = movingForward ? myBogie.frontCoupler : myBogie.rearCoupler;

        // Защита от зацикливания
        HashSet<TrainBogie> visited = new HashSet<TrainBogie>();
        visited.Add(currentBogie);

        // Пока есть сцепка и она к чему-то присоединена - идем дальше
        while (currentExitCoupler != null && currentExitCoupler.IsCoupled)
        {
            // Переходим в следующий вагон
            TrainCoupler nextIncomingCoupler = currentExitCoupler.connectedCoupler;
            TrainBogie nextBogie = nextIncomingCoupler.myBogie;

            if (visited.Contains(nextBogie)) break; // Защита от круга
            visited.Add(nextBogie);

            currentBogie = nextBogie;

            // Находим выходную сцепку этого вагона (противоположную той, через которую вошли)
            currentExitCoupler = (nextIncomingCoupler == nextBogie.frontCoupler)
                                 ? nextBogie.rearCoupler
                                 : nextBogie.frontCoupler;
        }

        // Возвращаем последнюю найденную сцепку (которая смотрит в пустоту)
        return currentExitCoupler;
    }

    void HandleImpact(TrainBogie targetBogie)
    {
        WagonPhysics wagonPhys = targetBogie.GetComponent<WagonPhysics>();
        LocomotiveController otherLoco = targetBogie.GetComponent<LocomotiveController>();

        if (wagonPhys == null && otherLoco == null)
        {
            currentSpeed = 0;
            SetThrottle(0); // Используем метод для сброса
            return;
        }

        Debug.Log($"БАМ! Столкновение с {targetBogie.name}");

        float impactVelocity = currentSpeed * 0.8f;

        if (wagonPhys != null) wagonPhys.ApplyImpulse(impactVelocity);
        else if (otherLoco != null) otherLoco.currentSpeed = impactVelocity;

        if (Mathf.Abs(currentSpeed) > 5f)
        {
            currentSpeed = -currentSpeed * bounceFactor;
            SetThrottle(0); // Сброс
        }
        else
        {
            currentSpeed = 0;
            SetThrottle(0); // Сброс
        }
    }

    /// <summary>
    /// Вспомогательный метод для установки тяги и уведомления подписчиков (пульта).
    /// </summary>
    public void SetThrottle(int value)
    {
        if (throttleInput != value)
        {
            throttleInput = value;
            // ОПОВЕЩАЕМ ВСЕХ (ПУЛЬТ), ЧТО ПЕРЕДАЧА ИЗМЕНИЛАСЬ
            OnGearChanged?.Invoke(throttleInput);
        }
    }

    // --- ЛОГИКА ДВИЖЕНИЯ ВАГОНОВ (БЕЗ ИЗМЕНЕНИЙ) ---
    void PullNeighbor(TrainBogie hostBogie, TrainCoupler hostCoupler, float predictedMoveDist)
    {
        TrainCoupler neighborCoupler = hostCoupler.connectedCoupler;
        if (neighborCoupler == null) return;

        TrainBogie neighborBogie = neighborCoupler.myBogie;
        if (processedBogies.Contains(neighborBogie)) return;
        processedBogies.Add(neighborBogie);

        float startGuessDist = neighborBogie.distanceOnRail + predictedMoveDist;
        neighborBogie.UpdatePosition(hostBogie.currentRail, startGuessDist);
        float neighborDist = startGuessDist;

        int iterations = 5;
        float delta = 0.01f;

        for (int i = 0; i < iterations; i++)
        {
            float currentGap = Vector3.Distance(hostCoupler.transform.position, neighborCoupler.transform.position);
            float error = currentGap - couplingGap;

            if (Mathf.Abs(error) < 0.0001f) break;

            neighborBogie.UpdatePosition(hostBogie.currentRail, neighborDist + delta);
            float newGap = Vector3.Distance(hostCoupler.transform.position, neighborCoupler.transform.position);

            float gradient = (newGap - currentGap) / delta;
            float adjustment = 0f;

            if (Mathf.Abs(gradient) < 0.001f) adjustment = error * Mathf.Sign(error) * 0.5f;
            else adjustment = -error / gradient;

            adjustment = Mathf.Clamp(adjustment, -0.2f, 0.2f);
            neighborDist += adjustment;
            neighborBogie.UpdatePosition(hostBogie.currentRail, neighborDist);
        }

        TrainCoupler nextCoupler = (neighborCoupler == neighborBogie.frontCoupler) ? neighborBogie.rearCoupler : neighborBogie.frontCoupler;
        if (nextCoupler != null && nextCoupler.IsCoupled)
        {
            PullNeighbor(neighborBogie, nextCoupler, predictedMoveDist);
        }
    }

    public void EmergencyStop()
    {
        currentSpeed = 0;
        SetThrottle(0);
    }

    // --- ВИЗУАЛИЗАЦИЯ В РЕДАКТОРЕ ---
    private void OnDrawGizmos()
    {
        // Безопасное получение ссылки в редакторе
        if (myBogie == null) myBogie = GetComponent<TrainBogie>();
        if (myBogie == null) return;

        // Рисуем сенсоры для обоих направлений
        DrawGizmoForDirection(true, Color.yellow); // Вперед
        DrawGizmoForDirection(false, Color.red);   // Назад
    }

    void DrawGizmoForDirection(bool forward, Color color)
    {
        // Используем ту же логику поиска крайней сцепки
        TrainCoupler sensor = GetExtremeCoupler(forward);

        if (sensor != null && !sensor.IsCoupled)
        {
            Vector3 outwardDir = (sensor.transform.position - sensor.myBogie.transform.position).normalized;
            float offsetInside = 0.2f;
            Vector3 origin = sensor.transform.position - (outwardDir * offsetInside);
            Vector3 endPos = origin + (outwardDir * collisionRayDistance);

            Gizmos.color = color;
            // Линия центра луча
            Gizmos.DrawLine(origin, endPos);
            // Сфера в конце (область удара)
            Gizmos.DrawWireSphere(endPos, collisionRayRadius);
            // Сфера в начале (старт проверки)
            Gizmos.DrawWireSphere(origin, collisionRayRadius);
        }
    }
}