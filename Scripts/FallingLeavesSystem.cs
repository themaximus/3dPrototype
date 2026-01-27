using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GlobalLeafSystem : MonoBehaviour
{
    [Header("Настройки Спрайта")]
    public Sprite leafSprite;
    public Material fallingLeafMaterial;

    [Header("Настройки Поиска")]
    public Material treeLeafMaterial;
    public LayerMask treeLayer = 1;

    [Header("Система")]
    public int maxLeaves = 2000;
    public float spawnRate = 10f;
    public float maxSpawnDistance = 50f;

    [Header("Физика Падения")]
    public bool faceCamera = true; // <-- НОВАЯ ГАЛОЧКА (Включите её!)
    public float gravitySpeed = 1.5f;
    public float swayFrequency = 2.0f;
    public float swayAmplitude = 0.5f;
    public float rotationSpeed = 100.0f;
    public float baseSize = 0.2f;

    private class Leaf
    {
        public Vector3 position;
        public Quaternion rotation; // Итоговый поворот для 3D
        public float spinAngle;     // Угол вращения для 2D (Billboard)
        public float timeOffset;
        public float speedMult;
        public bool isActive;
        public Vector3 axis;        // Ось вращения (для 3D режима)
    }

    private List<Bounds> treeZones = new List<Bounds>();
    private List<Leaf> leaves = new List<Leaf>();
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    private Mesh leafMesh;

    private const int BATCH_SIZE = 1023;
    private float spawnTimer;

    void Start()
    {
        GenerateMeshFromSprite();

        if (fallingLeafMaterial != null && leafSprite != null)
        {
            fallingLeafMaterial.mainTexture = leafSprite.texture;
        }

        FindAllTrees();
        InitializePool();
    }

    void GenerateMeshFromSprite()
    {
        leafMesh = new Mesh();
        leafMesh.name = "GeneratedLeafQuad";

        float width = baseSize;
        float height = baseSize;

        if (leafSprite != null)
        {
            float ratio = leafSprite.bounds.size.x / leafSprite.bounds.size.y;
            if (ratio > 1) height /= ratio;
            else width *= ratio;
        }

        float w = width * 0.5f;
        float h = height * 0.5f;

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-w, -h, 0), new Vector3(w, -h, 0),
            new Vector3(-w, h, 0), new Vector3(w, h, 0)
        };

        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 1)
        };

        int[] tris = new int[] { 0, 2, 1, 2, 3, 1 };

        // Нормали смотрят назад (-Z), чтобы при LookRotation к камере они смотрели НА камеру
        Vector3[] normals = new Vector3[4];
        for (int i = 0; i < 4; i++) normals[i] = -Vector3.forward;

        leafMesh.vertices = vertices;
        leafMesh.uv = uvs;
        leafMesh.triangles = tris;
        leafMesh.normals = normals;
    }

    void FindAllTrees()
    {
        treeZones.Clear();
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        foreach (var rend in allRenderers)
        {
            if (((1 << rend.gameObject.layer) & treeLayer) == 0) continue;
            foreach (var mat in rend.sharedMaterials)
            {
                if (mat == treeLeafMaterial)
                {
                    treeZones.Add(rend.bounds);
                    break;
                }
            }
        }
    }

    void InitializePool()
    {
        leaves.Clear();
        for (int i = 0; i < maxLeaves; i++) leaves.Add(new Leaf { isActive = false });
    }

    void Update()
    {
        if (fallingLeafMaterial == null || treeZones.Count == 0) return;
        if (leafMesh == null) GenerateMeshFromSprite();

        SpawnLogic();
        UpdatePhysics();
        RenderBatch();
    }

    void SpawnLogic()
    {
        spawnTimer += Time.deltaTime;
        float delay = 1.0f / spawnRate;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 camPos = cam.transform.position;

        while (spawnTimer >= delay)
        {
            spawnTimer -= delay;
            SpawnOneLeaf(camPos);
        }
    }

    void SpawnOneLeaf(Vector3 centerPos)
    {
        Leaf l = leaves.Find(x => !x.isActive);
        if (l == null) return;

        for (int i = 0; i < 10; i++)
        {
            Bounds randomTree = treeZones[Random.Range(0, treeZones.Count)];
            if (Vector3.Distance(randomTree.center, centerPos) < maxSpawnDistance)
            {
                Vector3 pos = new Vector3(
                    Random.Range(randomTree.min.x, randomTree.max.x),
                    Random.Range(randomTree.min.y, randomTree.max.y),
                    Random.Range(randomTree.min.z, randomTree.max.z)
                );

                l.isActive = true;
                l.position = pos;

                // Сброс параметров
                l.spinAngle = Random.Range(0f, 360f);
                l.rotation = Random.rotation; // Для 3D режима
                l.axis = Random.onUnitSphere; // Для 3D режима

                l.timeOffset = Random.Range(0f, 100f);
                l.speedMult = Random.Range(0.8f, 1.2f);
                return;
            }
        }
    }

    void UpdatePhysics()
    {
        float dt = Time.deltaTime;
        float time = Time.time;

        Camera cam = Camera.main;
        Quaternion camRot = (cam != null) ? cam.transform.rotation : Quaternion.identity;

        foreach (var l in leaves)
        {
            if (!l.isActive) continue;

            // 1. Движение
            l.position.y -= gravitySpeed * l.speedMult * dt;
            float sway = Mathf.Sin(time * swayFrequency + l.timeOffset) * swayAmplitude * dt;
            l.position.x += sway;
            l.position.z += Mathf.Cos(time * swayFrequency * 0.5f + l.timeOffset) * swayAmplitude * dt;

            // 2. Вращение (Логика переключения)
            if (faceCamera)
            {
                // РЕЖИМ BILLBOARD: Смотрим в камеру + крутимся вокруг Z
                l.spinAngle += rotationSpeed * dt;
                l.rotation = camRot * Quaternion.Euler(0, 0, l.spinAngle);
            }
            else
            {
                // РЕЖИМ 3D: Хаотичное вращение
                l.rotation *= Quaternion.AngleAxis(rotationSpeed * dt, l.axis);
            }

            // 3. Удаление
            if (l.position.y < -10f) l.isActive = false;
        }
    }

    void RenderBatch()
    {
        matrices.Clear();
        Vector3 scaleVec = Vector3.one;

        foreach (var l in leaves)
        {
            if (l.isActive)
            {
                matrices.Add(Matrix4x4.TRS(l.position, l.rotation, scaleVec));
            }
        }

        if (matrices.Count > 0)
        {
            for (int i = 0; i < matrices.Count; i += BATCH_SIZE)
            {
                int count = Mathf.Min(BATCH_SIZE, matrices.Count - i);
                Graphics.DrawMeshInstanced(leafMesh, 0, fallingLeafMaterial, matrices.GetRange(i, count), null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
        }
    }

    [ContextMenu("Refresh Trees")]
    public void RefreshTreeList()
    {
        FindAllTrees();
    }
}