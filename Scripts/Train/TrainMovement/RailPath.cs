using UnityEngine;
using System.Collections.Generic;

public class RailPath : MonoBehaviour
{
    public Color debugColor = Color.yellow;
    public bool loop = false;
    [Tooltip("Количество сегментов на один участок рельс. Увеличьте для более плавных поворотов.")]
    public int resolution = 20; // Увеличили дефолтное значение для плавности

    [HideInInspector] public bool isBranch = false;

    // Ссылка на физическую точку родителя (старый метод)
    [HideInInspector] public Transform phantomStartPoint;

    // НОВОЕ: Виртуальная точка для идеального математического стыка
    [HideInInspector] public Vector3? calculatedPhantomPoint;

    public float TotalLength { get; private set; } = 0;

    public List<RailPath> childBranches = new List<RailPath>();

    private List<float> arcLengths = new List<float>();
    private List<Transform> waypoints = new List<Transform>();

    private void Awake() { RecalculatePath(); }

    private void OnValidate()
    {
        // Не пересчитываем в рантайме каждый кадр, только при изменениях
        if (!Application.isPlaying) RecalculatePath();
    }

    [ContextMenu("Recalculate Path")]
    public void RecalculatePath()
    {
        waypoints.Clear();
        childBranches.Clear();

        if (isBranch)
        {
            waypoints.Add(transform);
        }

        // Собираем точки
        List<Transform> currentChildren = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Track_Container")) continue;
            if (!child.gameObject.activeInHierarchy) continue;
            currentChildren.Add(child);
        }

        for (int i = 0; i < currentChildren.Count; i++)
        {
            Transform child = currentChildren[i];
            waypoints.Add(child);

            // Проверяем на ветвление
            if (HasValidChildren(child))
            {
                RailPath branchPath = child.GetComponent<RailPath>();
                if (branchPath == null) branchPath = child.gameObject.AddComponent<RailPath>();

                branchPath.isBranch = true;
                branchPath.debugColor = Color.cyan;
                branchPath.resolution = this.resolution;

                // --- НОВАЯ ЛОГИКА ПЛАВНЫХ СТЫКОВ ---
                // Вместо того чтобы просто брать предыдущую точку, мы рассчитываем вектор
                if (waypoints.Count >= 2)
                {
                    // 1. Получаем предыдущую точку на ОСНОВНОМ пути
                    Transform prevPoint = waypoints[waypoints.Count - 2];

                    // 2. Вычисляем вектор движения (касательную) в точке стыка
                    Vector3 incomingTangent = (child.position - prevPoint.position).normalized;

                    // 3. Находим первую точку на ВЕТКЕ, чтобы понять масштаб изгиба
                    float branchSegmentLength = 1.0f; // Дефолт
                    foreach (Transform branchChild in child)
                    {
                        if (!branchChild.name.StartsWith("Track_Container") && branchChild.gameObject.activeInHierarchy)
                        {
                            branchSegmentLength = Vector3.Distance(child.position, branchChild.position);
                            break;
                        }
                    }

                    // 4. Создаем "Идеальную фантомную точку" строго позади стыка по вектору движения.
                    // Это заставит Catmull-Rom сплайн выйти из стыка прямо, а потом плавно повернуть.
                    branchPath.calculatedPhantomPoint = child.position - (incomingTangent * branchSegmentLength);

                    // Для совместимости оставляем ссылку
                    branchPath.phantomStartPoint = prevPoint;
                }
                else if (this.phantomStartPoint != null || this.calculatedPhantomPoint.HasValue)
                {
                    // Если это ветка от ветки, передаем параметры дальше
                    branchPath.phantomStartPoint = this.phantomStartPoint;
                    branchPath.calculatedPhantomPoint = this.calculatedPhantomPoint;
                }

                childBranches.Add(branchPath);

                branchPath.RecalculatePath();
            }
        }

        CalculateLengths();
    }

    private bool HasValidChildren(Transform t)
    {
        foreach (Transform child in t)
        {
            if (!child.name.StartsWith("Track_Container")) return true;
        }
        return false;
    }

    private void CalculateLengths()
    {
        int count = waypoints.Count;
        arcLengths.Clear();
        TotalLength = 0;

        if (count < 2) return;

        int segments = loop ? count : count - 1;

        for (int i = 0; i < segments; i++)
        {
            float segmentLength = 0;
            Vector3 prevPos = GetPoint(i, 0);

            for (int j = 1; j <= resolution; j++)
            {
                float t = (float)j / resolution;
                Vector3 nextPos = GetPoint(i, t);
                segmentLength += Vector3.Distance(prevPos, nextPos);
                prevPos = nextPos;
            }

            arcLengths.Add(segmentLength);
            TotalLength += segmentLength;
        }
    }

    // --- МЕТОД ПОЛУЧЕНИЯ ПОЗИЦИИ С ПРИОРИТЕТОМ НА ВИРТУАЛЬНУЮ ТОЧКУ ---
    private Vector3 GetPointPos(int index)
    {
        int count = waypoints.Count;
        if (count == 0) return transform.position;

        if (loop)
        {
            index = (index + count) % count;
            return waypoints[index].position;
        }
        else
        {
            // Точка "перед началом" (-1)
            if (index < 0)
            {
                // 1. Если рассчитана идеальная точка (New Logic), используем её
                if (calculatedPhantomPoint.HasValue) return calculatedPhantomPoint.Value;

                // 2. Если есть ссылка на трансформ (Old Logic), используем его
                if (phantomStartPoint != null) return phantomStartPoint.position;

                // 3. Иначе экстраполяция
                return waypoints[0].position - (waypoints[1].position - waypoints[0].position);
            }

            // Точка "после конца"
            if (index >= count)
            {
                int last = count - 1;
                return waypoints[last].position + (waypoints[last].position - waypoints[last - 1].position);
            }

            return waypoints[index].position;
        }
    }

    public Vector3 GetPoint(int i, float t)
    {
        Vector3 p0 = GetPointPos(i - 1);
        Vector3 p1 = GetPointPos(i);
        Vector3 p2 = GetPointPos(i + 1);
        Vector3 p3 = GetPointPos(i + 2);

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

    public Vector3 GetVelocity(int i, float t)
    {
        Vector3 p0 = GetPointPos(i - 1);
        Vector3 p1 = GetPointPos(i);
        Vector3 p2 = GetPointPos(i + 1);
        Vector3 p3 = GetPointPos(i + 2);

        return 0.5f * (
            (-p0 + p2) +
            (2f * (2f * p0 - 5f * p1 + 4f * p2 - p3)) * t +
            (3f * (-p0 + 3f * p1 - 3f * p2 + p3)) * t * t
        );
    }

    public void GetPointAtDistance(float distance, out Vector3 position, out Quaternion rotation)
    {
        if (waypoints == null || waypoints.Count == 0) RecalculatePath(); // Safety check

        if (arcLengths.Count == 0) { position = transform.position; rotation = transform.rotation; return; }

        if (loop) distance = Mathf.Repeat(distance, TotalLength);
        else distance = Mathf.Clamp(distance, 0, TotalLength);

        float accumulatedDist = 0;
        for (int i = 0; i < arcLengths.Count; i++)
        {
            float len = arcLengths[i];
            if (accumulatedDist + len >= distance)
            {
                float t = (distance - accumulatedDist) / len;

                position = GetPoint(i, t);
                Vector3 direction = GetVelocity(i, t).normalized;
                rotation = (direction != Vector3.zero) ? Quaternion.LookRotation(direction) : Quaternion.identity;
                return;
            }
            accumulatedDist += len;
        }

        position = GetPoint(arcLengths.Count - 1, 1);
        rotation = Quaternion.LookRotation(GetVelocity(arcLengths.Count - 1, 1));
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = debugColor;

        // Рисуем линию к виртуальной точке для отладки
        if (isBranch && calculatedPhantomPoint.HasValue)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f); // Красная линия - идеальный вектор входа
            Gizmos.DrawLine(calculatedPhantomPoint.Value, waypoints[0].position);
            Gizmos.DrawSphere(calculatedPhantomPoint.Value, 0.1f);
            Gizmos.color = debugColor;
        }

        int segments = loop ? waypoints.Count : waypoints.Count - 1;
        for (int i = 0; i < segments; i++)
        {
            Vector3 prev = GetPoint(i, 0);
            for (int j = 1; j <= resolution; j++)
            {
                Vector3 next = GetPoint(i, (float)j / resolution);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            if (!isBranch || i > 0) Gizmos.DrawSphere(waypoints[i].position, 0.2f);
        }
    }
}
