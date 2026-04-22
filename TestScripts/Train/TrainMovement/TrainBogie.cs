using UnityEngine;

public class TrainBogie : MonoBehaviour
{
    [Header("Настройки Пути")]
    public RailPath currentRail;
    public float distanceOnRail;

    [Header("Главные Тележки (Колесные пары)")]
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
            Debug.LogError($"[TrainBogie] {name}: Не назначены bogieFront или bogieRear! Скрипт отключен.");
            this.enabled = false;
            return;
        }

        bogieSpacing = Vector3.Distance(bogieFront.position, bogieRear.position);
        localBogieFrontPos = bogieFront.localPosition;
        localBogieRearPos = bogieRear.localPosition;
    }

    void Start()
    {
        if (currentRail != null) ForceUpdatePosition();
    }

    /// <summary>
    /// Главный метод движения.
    /// delta - смещение в метрах (например, speed * Time.deltaTime).
    /// </summary>
    public void MoveAlongRail(float delta)
    {
        if (currentRail == null || isLockedByTipper) return;

        float oldDist = distanceOnRail;
        float newDist = distanceOnRail + delta;

        // 1. ДВИЖЕНИЕ ВПЕРЕД (delta > 0)
        if (delta > 0)
        {
            // Проверяем, не пересекли ли мы стрелку (Junction) в этом кадре
            foreach (var junction in currentRail.junctions)
            {
                // Если мы были ДО стрелки, а стали ПОСЛЕ (или ровно на ней)
                if (oldDist <= junction.distanceOnRail && newDist > junction.distanceOnRail)
                {
                    // Проверяем, открыта ли стрелка
                    if (junction.branchPath != null && junction.branchPath.isSwitchOpen)
                    {
                        SwitchToBranch(junction, newDist);
                        return;
                    }
                }
            }

            // Проверяем конец пути (если это не ветка, а просто конец)
            if (newDist >= currentRail.TotalLength)
            {
                if (currentRail.loop)
                {
                    newDist = Mathf.Repeat(newDist, currentRail.TotalLength);
                }
                else
                {
                    newDist = currentRail.TotalLength; // Тупик
                }
            }
        }
        // 2. ДВИЖЕНИЕ НАЗАД (delta < 0)
        else
        {
            // Если мы ушли в минус (меньше 0)
            if (newDist < 0)
            {
                if (currentRail.parentPath != null)
                {
                    SwitchToParent(newDist);
                    return;
                }
                else if (currentRail.loop)
                {
                    newDist = Mathf.Repeat(newDist, currentRail.TotalLength);
                }
                else
                {
                    newDist = 0; // Тупик в начале
                }
            }
        }

        // Применяем новую дистанцию (если не произошло переключения)
        distanceOnRail = newDist;
        UpdateTransformVisuals();
    }

    // Логика перехода НА ВЕТКУ
    private void SwitchToBranch(RailPath.JunctionInfo junction, float currentDist)
    {
        // 1. Считаем, сколько мы "перелетели" за точку стрелки
        float overshoot = currentDist - junction.distanceOnRail;

        // 2. Меняем рельс
        currentRail = junction.branchPath;

        // 3. На ветке отсчет начинается с 0, поэтому наша позиция = перелет
        distanceOnRail = overshoot;

        // Важно: сразу обновляем визуал, чтобы не было "мигания"
        UpdateTransformVisuals();
    }

    // Логика возврата НА РОДИТЕЛЬСКИЙ ПУТЬ
    private void SwitchToParent(float currentDist) // currentDist здесь отрицательный (напр. -0.5)
    {
        // 1. Запоминаем точку стыка на родителе
        float junctionPoint = currentRail.startDistanceOnParent;
        RailPath parent = currentRail.parentPath;

        // 2. Меняем рельс
        currentRail = parent;

        // 3. Наша позиция = точка стыка - сколько мы отъехали назад
        // (т.к. currentDist отрицательный, мы просто складываем)
        distanceOnRail = junctionPoint + currentDist;

        UpdateTransformVisuals();
    }

    // Стандартный метод для жесткой установки позиции (например, при старте)
    public void UpdatePosition(RailPath rail, float dist)
    {
        currentRail = rail;
        distanceOnRail = dist;
        UpdateTransformVisuals();
    }

    public void ForceUpdatePosition()
    {
        UpdateTransformVisuals();
    }

    private void UpdateTransformVisuals()
    {
        if (currentRail == null) return;

        // Получаем точку А (передняя тележка)
        Vector3 posA; Quaternion rotA;
        currentRail.GetPointAtDistance(distanceOnRail, out posA, out rotA);

        // Ищем точку B (задняя тележка) на расстоянии bogieSpacing
        float distRear = distanceOnRail - bogieSpacing;

        // Обработка перехода через 0 для задней тележки
        // (Упрощенная: если задняя тележка вылезает за пределы текущего рельса,
        // IK может дернуться. Для идеальной плавности нужно рекурсивно искать точку на предыдущем рельсе,
        // но для прототипа это usually overkill. Если будет дергаться - скажи).
        if (currentRail.loop) distRear = Mathf.Repeat(distRear, currentRail.TotalLength);
        else distRear = Mathf.Clamp(distRear, 0, currentRail.TotalLength);

        Vector3 posB; Quaternion rotB;
        // Простая итерация для точного расстояния между осями (IK)
        float solverDist = distRear;
        for (int i = 0; i < 3; i++)
        {
            currentRail.GetPointAtDistance(solverDist, out posB, out rotB);
            float d = Vector3.Distance(posA, posB);
            if (Mathf.Abs(d - bogieSpacing) > 0.01f)
            {
                solverDist -= (bogieSpacing - d);
            }
        }
        currentRail.GetPointAtDistance(solverDist, out posB, out rotB);

        // Выравниваем корпус вагона
        Vector3 wagonDir = (posA - posB).normalized;

        // Усредняем Up вектор, чтобы вагон не крутило резко на стыках
        Vector3 wagonUp = (rotA * Vector3.up + rotB * Vector3.up).normalized;

        if (wagonDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(wagonDir, wagonUp) * Quaternion.Euler(rotationOffset);
        }

        // Ставим центр вагона
        Vector3 railCenter = (posA + posB) * 0.5f;
        Vector3 localCenter = (localBogieFrontPos + localBogieRearPos) * 0.5f;

        transform.position = railCenter - (transform.rotation * localCenter);
        transform.position += transform.up * heightOffset;

        // Двигаем сами модели тележек
        bogieFront.position = posA + (transform.up * heightOffset);
        bogieFront.rotation = rotA;

        bogieRear.position = posB + (transform.up * heightOffset);
        bogieRear.rotation = rotB;
    }
}