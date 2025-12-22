using UnityEngine;
using System.Collections.Generic;

public class RailPath : MonoBehaviour
{
    public Color debugColor = Color.yellow;
    public bool loop = false;
    public int resolution = 10;

    [HideInInspector] public bool isBranch = false;

    // НОВОЕ: Точка, откуда "виртуально" пришел поезд. Нужна для расчета инерции на старте ветки.
    [HideInInspector] public Transform phantomStartPoint;

    public float TotalLength { get; private set; } = 0;

    public List<RailPath> childBranches = new List<RailPath>();

    private List<float> arcLengths = new List<float>();
    private List<Transform> waypoints = new List<Transform>();

    private void Awake() { RecalculatePath(); }

    private void OnValidate()
    {
        RecalculatePath();
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

        // Собираем точки и ищем ветки
        // Используем for, чтобы иметь доступ к индексу и найти "предыдущую" точку
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

                // --- МАГИЯ ПЛАВНОСТИ ---
                // Передаем ветке точку, которая была ПЕРЕД развилкой.
                // Если развилка - это текущий child (waypoints[last]), то предыдущая - это waypoints[last-1].
                // Если мы только начали и child - первая точка, берем phantomStartPoint текущего пути.
                if (waypoints.Count >= 2)
                {
                    branchPath.phantomStartPoint = waypoints[waypoints.Count - 2];
                }
                else if (this.phantomStartPoint != null)
                {
                    branchPath.phantomStartPoint = this.phantomStartPoint;
                }

                childBranches.Add(branchPath);

                // Важно: сначала задали phantomPoint, потом пересчитали
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

    // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД ПОЛУЧЕНИЯ ПОЗИЦИИ ---
    // Вся логика плавности здесь: если просят точку -1, мы отдаем фантомную
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
            // Если просят точку "перед началом" (-1)
            if (index < 0)
            {
                // Если у нас есть фантомная точка (от родителя), используем её!
                if (phantomStartPoint != null) return phantomStartPoint.position;

                // Иначе экстраполируем (как раньше): берем первую точку
                return waypoints[0].position - (waypoints[1].position - waypoints[0].position);
            }

            // Если просят точку "после конца"
            if (index >= count)
            {
                // Экстраполяция конца
                return waypoints[count - 1].position + (waypoints[count - 1].position - waypoints[count - 2].position);
            }

            return waypoints[index].position;
        }
    }

    public Vector3 GetPoint(int i, float t)
    {
        // Используем новый метод получения координат
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
        if (waypoints == null || waypoints.Count == 0) RecalculatePath();

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

        // Рисуем линию к фантомной точке для отладки, чтобы видеть связь
        if (isBranch && phantomStartPoint != null)
        {
            Gizmos.color = new Color(1, 1, 1, 0.3f);
            Gizmos.DrawLine(phantomStartPoint.position, waypoints[0].position);
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