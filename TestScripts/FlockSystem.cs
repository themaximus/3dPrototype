using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class FlockSystemGPU : MonoBehaviour
{
    public enum RotationMode { CameraBillboard, World3D }
    public enum BoundaryMode { Soft, HardClamp } // <--- НОВЫЙ ПЕРЕКЛЮЧАТЕЛЬ

    // --- ВИЗУАЛ ---
    [Header("Настройки Отрисовки")]
    public Material birdMaterial;
    public Vector2 birdSize = new Vector2(0.5f, 0.5f);

    [Header("Режим Поворота")]
    public RotationMode rotationMode = RotationMode.CameraBillboard;
    [Tooltip("Коррекция для режима CameraBillboard")]
    [Range(-180, 180)] public float spriteRotation2D = 0f;
    [Tooltip("Коррекция для режима World3D")]
    public Vector3 spriteRotation3D = new Vector3(90f, 0f, 0f);

    [Header("Анимация")]
    public bool enableWingFlap = true;
    public float wingFps = 15f;
    [Range(0f, 1f)] public float wingStrength = 0.15f;

    // --- НАСТРОЙКИ СТАИ ---
    [Header("Общие Настройки")]
    [Range(1, 5000)] public int birdCount = 200;
    public float spawnRadius = 20f;

    [Header("Движение")]
    public float minSpeed = 2f;
    public float maxSpeed = 6f;
    public float maxTurnRate = 120f;

    [Header("Радиусы Восприятия")]
    public float cohesionRadius = 4f;
    public float alignmentRadius = 4f;
    public float separationRadius = 1.2f;

    [Header("Веса (Сила поведения)")]
    public float cohesionWeight = 1f;
    public float alignmentWeight = 1f;
    public float separationWeight = 1.5f;
    public float boundsWeight = 5f;
    public float jitterWeight = 0.4f;

    [Header("Режимы Поведения")]
    public BehaviorMode behaviorMode = BehaviorMode.Realistic;
    public enum BehaviorMode { Realistic, Simplified, LineFlight, Panic, Circle }

    [Header("Границы")]
    public BoundaryMode boundaryMode = BoundaryMode.HardClamp; // <--- ВЫБОР РЕЖИМА
    public Vector3 boundsCenter = Vector3.zero;
    public Vector3 boundsSize = new Vector3(50f, 30f, 50f);
    public float boundsSafeMargin = 5f;

    private Vector3 circleCenter;

    // --- ВНУТРЕННИЕ ДАННЫЕ ---
    private class Boid
    {
        public Vector3 position;
        public Vector3 velocity;
        public float scaleMult;
        public float flapOffset;
        public List<Boid> neighbors = new List<Boid>();
    }

    private List<Boid> boids = new List<Boid>();
    private Mesh quadMesh;
    private const int BATCH_SIZE = 1023;

    private float neighborTimer = 0f;
    private float neighborUpdateInterval = 0.2f;
    private Vector3 globalFlockCenter;
    private Vector3 globalFlockVelocity;
    private List<Matrix4x4> drawMatrices = new List<Matrix4x4>();

    private void OnEnable()
    {
        if (quadMesh == null) GenerateQuadMesh();
        InitializeFlock();
    }

    private void Update()
    {
        if (birdMaterial == null) return;
        if (quadMesh == null) GenerateQuadMesh();

        if (Application.isPlaying)
        {
            neighborTimer -= Time.deltaTime;
            if (neighborTimer <= 0f)
            {
                neighborTimer = neighborUpdateInterval;
                RecalculateNeighbors();
            }
            UpdateBoidsLogic();
        }

        RenderBoids();
    }

    void InitializeFlock()
    {
        boids.Clear();
        Vector3 center = transform.position + boundsCenter;
        circleCenter = center;

        for (int i = 0; i < birdCount; i++)
        {
            Boid b = new Boid();
            b.position = center + Random.insideUnitSphere * spawnRadius;
            b.velocity = Random.onUnitSphere * Random.Range(minSpeed, maxSpeed);
            b.velocity.y = Mathf.Clamp(b.velocity.y, -0.5f, 0.5f);
            b.scaleMult = Random.Range(0.8f, 1.2f);
            b.flapOffset = Random.Range(0f, 100f);
            boids.Add(b);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && boids.Count != birdCount) InitializeFlock();
        minSpeed = Mathf.Max(0.1f, minSpeed);
    }

    void RecalculateNeighbors()
    {
        Vector3 centerSum = Vector3.zero;
        Vector3 velSum = Vector3.zero;
        int count = boids.Count;

        for (int i = 0; i < count; i++)
        {
            centerSum += boids[i].position;
            velSum += boids[i].velocity;
            boids[i].neighbors.Clear();
        }

        if (count > 0)
        {
            globalFlockCenter = centerSum / count;
            globalFlockVelocity = velSum / count;
        }

        float maxRadius = Mathf.Max(cohesionRadius, Mathf.Max(alignmentRadius, separationRadius));
        float sqrMaxRadius = maxRadius * maxRadius;

        for (int i = 0; i < count; i++)
        {
            int checks = Mathf.Min(count, 40);
            for (int k = 0; k < checks; k++)
            {
                int j = Random.Range(0, count);
                if (i == j) continue;

                if ((boids[i].position - boids[j].position).sqrMagnitude < sqrMaxRadius)
                {
                    boids[i].neighbors.Add(boids[j]);
                    if (boids[i].neighbors.Count >= 10) break;
                }
            }
        }
    }

    void UpdateBoidsLogic()
    {
        float dt = Time.deltaTime;
        Vector3 boundsMin = transform.position + boundsCenter - boundsSize * 0.5f;
        Vector3 boundsMax = transform.position + boundsCenter + boundsSize * 0.5f;

        for (int i = 0; i < boids.Count; i++)
        {
            Boid b = boids[i];
            Vector3 force = Vector3.zero;

            switch (behaviorMode)
            {
                case BehaviorMode.Realistic:
                    force += ApplyCohesion(b) * cohesionWeight;
                    force += ApplyAlignment(b) * alignmentWeight;
                    force += ApplySeparation(b) * separationWeight;
                    force += Random.insideUnitSphere * jitterWeight;
                    break;
                case BehaviorMode.Simplified:
                    Vector3 toCenter = (globalFlockCenter - b.position).normalized;
                    force += toCenter * cohesionWeight * 0.5f;
                    force += Random.insideUnitSphere * jitterWeight * 0.5f;
                    force += ApplySeparation(b) * separationWeight;
                    break;
                case BehaviorMode.LineFlight:
                    force += globalFlockVelocity.normalized * cohesionWeight;
                    force += ApplyAlignment(b) * alignmentWeight;
                    force += Random.insideUnitSphere * jitterWeight * 0.2f;
                    break;
                case BehaviorMode.Panic:
                    force += Random.insideUnitSphere * jitterWeight * 4f;
                    force += ApplySeparation(b) * separationWeight * 3f;
                    break;
                case BehaviorMode.Circle:
                    Vector3 toCircle = circleCenter - b.position;
                    Vector3 tangent = Vector3.Cross(toCircle.normalized, Vector3.up);
                    force += tangent * cohesionWeight * 2f;
                    force += toCircle.normalized * 0.5f;
                    force += ApplySeparation(b) * separationWeight;
                    break;
            }

            // Мягкое руление от стен (работает всегда, если вес > 0)
            force += ApplyBounds(b, boundsMin, boundsMax) * boundsWeight;

            Vector3 desiredVel = b.velocity + force * dt;
            float currentSpeed = desiredVel.magnitude;
            currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

            Vector3 newDir = Vector3.RotateTowards(b.velocity.normalized, desiredVel.normalized, maxTurnRate * Mathf.Deg2Rad * dt, 0f);

            b.velocity = newDir * currentSpeed;
            b.position += b.velocity * dt;

            // --- ЛОГИКА ГРАНИЦ (НОВОЕ) ---
            if (boundaryMode == BoundaryMode.HardClamp)
            {
                // Если режим "Клетка" - жестко ограничиваем и отражаем скорость
                if (b.position.x < boundsMin.x || b.position.x > boundsMax.x)
                {
                    b.position.x = Mathf.Clamp(b.position.x, boundsMin.x, boundsMax.x);
                    b.velocity.x = -b.velocity.x;
                }
                if (b.position.y < boundsMin.y || b.position.y > boundsMax.y)
                {
                    b.position.y = Mathf.Clamp(b.position.y, boundsMin.y, boundsMax.y);
                    b.velocity.y = -b.velocity.y;
                }
                if (b.position.z < boundsMin.z || b.position.z > boundsMax.z)
                {
                    b.position.z = Mathf.Clamp(b.position.z, boundsMin.z, boundsMax.z);
                    b.velocity.z = -b.velocity.z;
                }
            }
            // Если режим "Soft" - ничего не делаем, птица может улететь, если сила boundsWeight слабая
        }
    }

    Vector3 ApplyCohesion(Boid b)
    {
        if (b.neighbors.Count == 0) return Vector3.zero;
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (var n in b.neighbors)
        {
            if (Vector3.Distance(b.position, n.position) < cohesionRadius) { center += n.position; count++; }
        }
        if (count == 0) return Vector3.zero;
        center /= count;
        return (center - b.position).normalized;
    }

    Vector3 ApplyAlignment(Boid b)
    {
        if (b.neighbors.Count == 0) return Vector3.zero;
        Vector3 avgVel = Vector3.zero;
        int count = 0;
        foreach (var n in b.neighbors)
        {
            if (Vector3.Distance(b.position, n.position) < alignmentRadius) { avgVel += n.velocity; count++; }
        }
        if (count == 0) return Vector3.zero;
        return (avgVel.normalized - b.velocity.normalized);
    }

    Vector3 ApplySeparation(Boid b)
    {
        if (b.neighbors.Count == 0) return Vector3.zero;
        Vector3 repel = Vector3.zero;
        foreach (var n in b.neighbors)
        {
            Vector3 diff = b.position - n.position;
            float d = diff.magnitude;
            if (d < separationRadius && d > 0.001f)
            {
                repel += diff.normalized / d;
            }
        }
        return repel.normalized;
    }

    Vector3 ApplyBounds(Boid b, Vector3 min, Vector3 max)
    {
        Vector3 pos = b.position;
        Vector3 steer = Vector3.zero;
        if (pos.x < min.x + boundsSafeMargin) steer += Vector3.right * (1f - (pos.x - min.x) / boundsSafeMargin);
        if (pos.x > max.x - boundsSafeMargin) steer += Vector3.left * (1f - (max.x - pos.x) / boundsSafeMargin);
        if (pos.y < min.y + boundsSafeMargin) steer += Vector3.up * (1f - (pos.y - min.y) / boundsSafeMargin);
        if (pos.y > max.y - boundsSafeMargin) steer += Vector3.down * (1f - (max.y - pos.y) / boundsSafeMargin);
        if (pos.z < min.z + boundsSafeMargin) steer += Vector3.forward * (1f - (pos.z - min.z) / boundsSafeMargin);
        if (pos.z > max.z - boundsSafeMargin) steer += Vector3.back * (1f - (pos.z - min.z) / boundsSafeMargin);
        return steer * 5f;
    }

    void RenderBoids()
    {
        drawMatrices.Clear();
        Vector3 baseScale = new Vector3(birdSize.x, birdSize.y, 1f);

        Camera cam = null;
        if (Application.isPlaying) cam = Camera.main;
#if UNITY_EDITOR
        else cam = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera : null;
#endif
        if (cam == null) return;

        for (int i = 0; i < boids.Count; i++)
        {
            Boid b = boids[i];
            Quaternion rotation = Quaternion.identity;

            if (rotationMode == RotationMode.CameraBillboard)
            {
                Quaternion billboardRot = cam.transform.rotation;
                Vector3 localVel = cam.transform.InverseTransformDirection(b.velocity);
                float angle = Mathf.Atan2(localVel.x, localVel.y) * Mathf.Rad2Deg;
                rotation = billboardRot * Quaternion.Euler(0, 0, -angle + spriteRotation2D);
            }
            else
            {
                if (b.velocity.sqrMagnitude > 0.01f)
                {
                    rotation = Quaternion.LookRotation(b.velocity.normalized);
                    rotation *= Quaternion.Euler(spriteRotation3D);
                }
            }

            Vector3 currentScale = baseScale * b.scaleMult;
            if (enableWingFlap)
            {
                float flap = Mathf.Sin(Time.time * wingFps + b.flapOffset);
                currentScale.x *= 1.0f + flap * wingStrength;
            }

            drawMatrices.Add(Matrix4x4.TRS(b.position, rotation, currentScale));
        }

        if (drawMatrices.Count > 0)
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            for (int i = 0; i < drawMatrices.Count; i += BATCH_SIZE)
            {
                int count = Mathf.Min(BATCH_SIZE, drawMatrices.Count - i);
                Graphics.DrawMeshInstanced(quadMesh, 0, birdMaterial, drawMatrices.GetRange(i, count), props, UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
            }
        }
    }

    void GenerateQuadMesh()
    {
        quadMesh = new Mesh();
        quadMesh.name = "BirdQuad";
        float w = 0.5f; float h = 0.5f;
        Vector3[] vertices = new Vector3[] { new Vector3(-w, -h, 0), new Vector3(w, -h, 0), new Vector3(-w, h, 0), new Vector3(w, h, 0) };
        Vector2[] uvs = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        int[] tris = new int[] { 0, 2, 1, 2, 3, 1 };
        Vector3[] normals = new Vector3[4];
        for (int i = 0; i < 4; i++) normals[i] = -Vector3.forward;
        quadMesh.vertices = vertices; quadMesh.uv = uvs; quadMesh.triangles = tris; quadMesh.normals = normals;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + boundsCenter, boundsSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + boundsCenter, spawnRadius);
        if (behaviorMode == BehaviorMode.Circle)
        {
            Gizmos.color = Color.red; Gizmos.DrawSphere(circleCenter, 1.0f);
        }
    }

    [ContextMenu("Set Circle Center Here")]
    void SetCirclePos() { circleCenter = transform.position; }
}