using UnityEngine;
using System.Collections.Generic;

public class RailPath : MonoBehaviour
{
    public Color debugColor = Color.yellow;
    public bool loop = false;
    public int resolution = 10;

    public float TotalLength { get; private set; } = 0;

    // Кэш длин сегментов
    private List<float> arcLengths = new List<float>();

    // Кэш валидных точек (без мусора типа Track_Container)
    private List<Transform> waypoints = new List<Transform>();

    private void Awake() { RecalculatePath(); }

    // Обновляем и в редакторе при изменении
    private void OnValidate()
    {
        // if (!Application.isPlaying) // Можно убрать проверку, чтобы видеть изменения сразу
        RecalculatePath();
    }

    [ContextMenu("Recalculate Path")]
    public void RecalculatePath()
    {
        // 1. Собираем только правильные точки
        waypoints.Clear();
        foreach (Transform child in transform)
        {
            // ИГНОРИРУЕМ контейнер с моделями
            if (child.name == "Track_Container") continue;

            // Также можно игнорировать выключенные объекты, если хотите скрывать точки
            if (!child.gameObject.activeInHierarchy) continue;

            waypoints.Add(child);
        }

        int count = waypoints.Count;
        if (count < 2) return;

        arcLengths.Clear();
        TotalLength = 0;

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

    // Математика Catmull-Rom (использует список waypoints вместо transform.GetChild)
    public Vector3 GetPoint(int i, float t)
    {
        int count = waypoints.Count;
        if (count == 0) return transform.position;

        int p0_idx = i - 1;
        int p1_idx = i;
        int p2_idx = i + 1;
        int p3_idx = i + 2;

        if (loop)
        {
            p0_idx = (p0_idx + count) % count;
            p1_idx = (p1_idx + count) % count;
            p2_idx = (p2_idx + count) % count;
            p3_idx = (p3_idx + count) % count;
        }
        else
        {
            p0_idx = Mathf.Clamp(p0_idx, 0, count - 1);
            p1_idx = Mathf.Clamp(p1_idx, 0, count - 1);
            p2_idx = Mathf.Clamp(p2_idx, 0, count - 1);
            p3_idx = Mathf.Clamp(p3_idx, 0, count - 1);
        }

        // Берем позиции из нашего отфильтрованного списка
        Vector3 p0 = waypoints[p0_idx].position;
        Vector3 p1 = waypoints[p1_idx].position;
        Vector3 p2 = waypoints[p2_idx].position;
        Vector3 p3 = waypoints[p3_idx].position;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

    public Vector3 GetVelocity(int i, float t)
    {
        int count = waypoints.Count;
        if (count == 0) return transform.forward;

        int p0_idx = i - 1; int p1_idx = i; int p2_idx = i + 1; int p3_idx = i + 2;

        if (loop)
        {
            p0_idx = (p0_idx + count) % count; p1_idx = (p1_idx + count) % count;
            p2_idx = (p2_idx + count) % count; p3_idx = (p3_idx + count) % count;
        }
        else
        {
            p0_idx = Mathf.Clamp(p0_idx, 0, count - 1); p1_idx = Mathf.Clamp(p1_idx, 0, count - 1);
            p2_idx = Mathf.Clamp(p2_idx, 0, count - 1); p3_idx = Mathf.Clamp(p3_idx, 0, count - 1);
        }

        Vector3 p0 = waypoints[p0_idx].position;
        Vector3 p1 = waypoints[p1_idx].position;
        Vector3 p2 = waypoints[p2_idx].position;
        Vector3 p3 = waypoints[p3_idx].position;

        return 0.5f * (
            (-p0 + p2) +
            (2f * (2f * p0 - 5f * p1 + 4f * p2 - p3)) * t +
            (3f * (-p0 + 3f * p1 - 3f * p2 + p3)) * t * t
        );
    }

    public void GetPointAtDistance(float distance, out Vector3 position, out Quaternion rotation)
    {
        // Если список пуст или не инициализирован (в редакторе бывает), пересчитываем
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
        // Для Гизмо тоже нужно убедиться, что список актуален
        if (waypoints == null || waypoints.Count != transform.childCount - (transform.Find("Track_Container") ? 1 : 0))
        {
            // Легкий пересчет только списка, чтобы не грузить редактор
            waypoints = new List<Transform>();
            foreach (Transform t in transform) if (t.name != "Track_Container") waypoints.Add(t);
        }

        Gizmos.color = debugColor;
        if (waypoints.Count < 2) return;

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
            // Рисуем сферы только на реальных вейпоинтах
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
        }
    }
}