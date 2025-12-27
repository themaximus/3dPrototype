using UnityEngine;
using System.Collections.Generic;

public class RailPath : MonoBehaviour
{
    public Color debugColor = Color.yellow;
    public bool loop = false;
    [Tooltip("Количество сегментов на один участок рельс.")]
    public int resolution = 20;

    [Header("Настройки Ветки")]
    public bool isBranch = false;
    [Tooltip("Открыта ли стрелка на эту ветку?")]
    public bool isSwitchOpen = false;

    // --- НОВЫЕ ПОЛЯ ДЛЯ ДВИЖЕНИЯ НАЗАД ---
    [Header("Связь с Родителем (Автоматически)")]
    public RailPath parentPath;          // Кто наш родитель?
    public float startDistanceOnParent;  // Где мы начинаемся на родителе?
    // -------------------------------------

    [HideInInspector] public Transform phantomStartPoint;
    [HideInInspector] public Vector3? calculatedPhantomPoint;

    public float TotalLength { get; private set; } = 0;

    [System.Serializable]
    public class JunctionInfo
    {
        public float distanceOnRail;
        public RailPath branchPath;
    }

    public List<JunctionInfo> junctions = new List<JunctionInfo>();

    public List<RailPath> childBranches = new List<RailPath>();
    private List<float> arcLengths = new List<float>();
    private List<Transform> waypoints = new List<Transform>();

    private void Awake() { RecalculatePath(); }

    private void OnValidate()
    {
        if (!Application.isPlaying) RecalculatePath();
    }

    [ContextMenu("Recalculate Path")]
    public void RecalculatePath()
    {
        waypoints.Clear();
        childBranches.Clear();
        junctions.Clear();

        // Сбрасываем родителя (на случай, если путь отцепили)
        parentPath = null;
        startDistanceOnParent = 0f;

        if (isBranch) waypoints.Add(transform);

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

            if (HasValidChildren(child))
            {
                RailPath branchPath = child.GetComponent<RailPath>();
                if (branchPath == null) branchPath = child.gameObject.AddComponent<RailPath>();

                branchPath.isBranch = true;
                branchPath.debugColor = Color.cyan;
                branchPath.resolution = this.resolution;

                if (waypoints.Count >= 2)
                {
                    Transform prevPoint = waypoints[waypoints.Count - 2];
                    Vector3 incomingTangent = (child.position - prevPoint.position).normalized;

                    float branchSegmentLength = 1.0f;
                    foreach (Transform branchChild in child)
                    {
                        if (!branchChild.name.StartsWith("Track_Container") && branchChild.gameObject.activeInHierarchy)
                        {
                            branchSegmentLength = Vector3.Distance(child.position, branchChild.position);
                            break;
                        }
                    }
                    branchPath.calculatedPhantomPoint = child.position - (incomingTangent * branchSegmentLength);
                    branchPath.phantomStartPoint = prevPoint;
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
            Transform currentPoint = waypoints[i];

            RailPath branchHere = childBranches.Find(b => b.transform == currentPoint);

            if (branchHere != null)
            {
                junctions.Add(new JunctionInfo
                {
                    distanceOnRail = TotalLength,
                    branchPath = branchHere
                });

                // --- ЗАПИСЫВАЕМ СЕБЯ КАК РОДИТЕЛЯ В ВЕТКУ ---
                branchHere.parentPath = this;
                branchHere.startDistanceOnParent = TotalLength;
                // ---------------------------------------------
            }

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
            if (index < 0)
            {
                if (calculatedPhantomPoint.HasValue) return calculatedPhantomPoint.Value;
                if (phantomStartPoint != null) return phantomStartPoint.position;
                return waypoints[0].position - (waypoints[1].position - waypoints[0].position);
            }
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

        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
    }

    public Vector3 GetVelocity(int i, float t)
    {
        Vector3 p0 = GetPointPos(i - 1);
        Vector3 p1 = GetPointPos(i);
        Vector3 p2 = GetPointPos(i + 1);
        Vector3 p3 = GetPointPos(i + 2);
        return 0.5f * ((-p0 + p2) + (2f * (2f * p0 - 5f * p1 + 4f * p2 - p3)) * t + (3f * (-p0 + 3f * p1 - 3f * p2 + p3)) * t * t);
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

        if (isBranch)
        {
            Gizmos.color = isSwitchOpen ? Color.green : Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2, 0.5f);
            Gizmos.color = debugColor;
        }

        if (isBranch && calculatedPhantomPoint.HasValue)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawLine(calculatedPhantomPoint.Value, waypoints[0].position);
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