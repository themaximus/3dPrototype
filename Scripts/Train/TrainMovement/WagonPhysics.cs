using UnityEngine;

[RequireComponent(typeof(TrainBogie))]
public class WagonPhysics : MonoBehaviour
{
    [Header("Физика Наката")]
    public float currentSpeed = 0f;
    [Tooltip("Как быстро вагон останавливается сам по себе (Трение металла)")]
    public float friction = 2f;
    [Tooltip("Минимальная скорость, при которой вагон мгновенно останавливается")]
    public float stopThreshold = 0.1f;

    [Header("Физика Столкновений")]
    [Tooltip("Максимальная дистанция луча (должна быть больше порога касания)")]
    public float collisionRayDistance = 1.0f;
    [Tooltip("Радиус луча детекции (толщина)")]
    public float collisionRayRadius = 0.5f;
    [Tooltip("Дистанция, при которой считается, что вагоны коснулись (в метрах)")]
    public float touchThreshold = 0.05f;
    [Tooltip("Какую часть скорости передать следующему вагону (0.6 = 60%, удар гасится)")]
    public float impactTransferFactor = 0.6f;
    [Tooltip("Коэффициент отскока при ударе")]
    public float bounceFactor = 0.3f;

    private TrainBogie myBogie;

    void Awake()
    {
        myBogie = GetComponent<TrainBogie>();
    }

    void Update()
    {
        // 1. Проверка: Если вагон прицеплен к составу, этот скрипт отключает свою физику
        // (Управление передается Локомотиву)
        bool isCoupled = (myBogie.frontCoupler != null && myBogie.frontCoupler.IsCoupled) ||
                         (myBogie.rearCoupler != null && myBogie.rearCoupler.IsCoupled);

        if (myBogie.isLocomotive || isCoupled)
        {
            currentSpeed = 0f;
            return;
        }

        // 2. Проверка столкновений (перед движением)
        CheckCollision();

        // 3. Движение по инерции
        if (Mathf.Abs(currentSpeed) > 0.001f)
        {
            float moveDist = currentSpeed * Time.deltaTime;

            if (myBogie.currentRail != null)
            {
                myBogie.UpdatePosition(myBogie.currentRail, myBogie.distanceOnRail + moveDist);
            }

            // Трение (плавная остановка)
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, friction * Time.deltaTime);

            if (Mathf.Abs(currentSpeed) < stopThreshold)
            {
                currentSpeed = 0f;
            }
        }
    }

    void CheckCollision()
    {
        if (Mathf.Abs(currentSpeed) < 0.1f) return;

        bool movingForward = currentSpeed > 0;

        // Для одиночного вагона проверяем соответствующую сцепку
        TrainCoupler sensorCoupler = movingForward ? myBogie.frontCoupler : myBogie.rearCoupler;

        if (sensorCoupler == null || sensorCoupler.IsCoupled) return;

        // Вектор "Наружу"
        Vector3 outwardDir = (sensorCoupler.transform.position - sensorCoupler.myBogie.transform.position).normalized;

        // Смещаем начало луча немного внутрь вагона для надежности
        float offsetInside = 0.2f;
        Vector3 origin = sensorCoupler.transform.position - (outwardDir * offsetInside);

        RaycastHit hit;
        // Визуализация луча (Зеленый = попал, Красный = мимо)
        if (Physics.SphereCast(origin, collisionRayRadius, outwardDir, out hit, collisionRayDistance))
        {
            TrainCoupler hitCoupler = hit.collider.GetComponent<TrainCoupler>();

            if (hitCoupler != null && hitCoupler.myBogie != myBogie)
            {
                // Расстояние от края сцепки до цели
                float realDistance = hit.distance - offsetInside;

                // Толкаем только если коснулись (дистанция <= touchThreshold)
                if (realDistance <= touchThreshold)
                {
                    HandleImpact(hitCoupler.myBogie);
                }
            }
        }
    }

    void HandleImpact(TrainBogie targetBogie)
    {
        WagonPhysics otherWagon = targetBogie.GetComponent<WagonPhysics>();
        LocomotiveController otherLoco = targetBogie.GetComponent<LocomotiveController>();

        // Если врезались в стену (нет скриптов физики) - мгновенная остановка
        if (otherWagon == null && otherLoco == null)
        {
            currentSpeed = 0;
            return;
        }

        Debug.Log($"[WagonPhysics] Удар об {targetBogie.name}");

        // Передача импульса
        float impactVelocity = currentSpeed * impactTransferFactor;

        if (otherWagon != null)
        {
            otherWagon.ApplyImpulse(impactVelocity);
        }
        else if (otherLoco != null)
        {
            // Если вагон врезался в локомотив - толкаем локомотив
            otherLoco.currentSpeed = impactVelocity;
        }

        // Реакция самого вагона
        if (Mathf.Abs(currentSpeed) > 2f) // Если удар был сильный
        {
            // Отскок
            currentSpeed = -currentSpeed * bounceFactor;
        }
        else
        {
            // Просто остановка
            currentSpeed = 0f;
        }
    }

    public void ApplyImpulse(float forceVelocity)
    {
        currentSpeed = forceVelocity;
    }

    // --- ВИЗУАЛИЗАЦИЯ В РЕДАКТОРЕ ---
    private void OnDrawGizmos()
    {
        if (myBogie == null) myBogie = GetComponent<TrainBogie>();
        if (myBogie == null) return;

        // Рисуем гизмо только если вагон свободен (не в составе поезда)
        bool isCoupled = (myBogie.frontCoupler != null && myBogie.frontCoupler.IsCoupled) ||
                         (myBogie.rearCoupler != null && myBogie.rearCoupler.IsCoupled);

        if (!isCoupled && !myBogie.isLocomotive)
        {
            DrawGizmoForDirection(true, Color.yellow);
            DrawGizmoForDirection(false, Color.red);
        }
    }

    void DrawGizmoForDirection(bool forward, Color color)
    {
        TrainCoupler sensor = forward ? myBogie.frontCoupler : myBogie.rearCoupler;

        if (sensor != null && !sensor.IsCoupled)
        {
            Vector3 outwardDir = (sensor.transform.position - sensor.myBogie.transform.position).normalized;
            float offsetInside = 0.2f;
            Vector3 origin = sensor.transform.position - (outwardDir * offsetInside);
            Vector3 endPos = origin + (outwardDir * collisionRayDistance);

            Gizmos.color = color;
            Gizmos.DrawLine(origin, endPos);
            Gizmos.DrawWireSphere(endPos, collisionRayRadius);
            Gizmos.DrawWireSphere(origin, collisionRayRadius);
        }
    }
}