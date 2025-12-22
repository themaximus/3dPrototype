using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RailPath))]
public class RailTrackGenerator : MonoBehaviour
{
    [Header("Ресурсы")]
    public GameObject trackSegmentPrefab;

    [Header("Настройки")]
    [Tooltip("Длина модели рельсы. ВАЖНО: Укажите точно.")]
    public float segmentLength = 1.0f;
    public float heightOffset = 0.0f;

    [Header("ВАЖНО: Ориентация")]
    [Tooltip("Поверните модель ЗДЕСЬ, чтобы Z смотрел вдоль пути, а Y вверх. (Например: 90, 0, 0 или -90, 0, 0)")]
    public Vector3 inputMeshRotation = Vector3.zero;

    [Tooltip("Финальная коррекция поворота на сплайне (обычно 0,0,0)")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Исправление Размера")]
    public Vector3 manualMeshScale = Vector3.one;

    [Header("Искривление")]
    public bool bendMeshesToSpline = true;

    [Header("Оптимизация")]
    public bool useOptimization = true;
    public int segmentsPerChunk = 50;

    private RailPath railPath;

    [ContextMenu("Generate Tracks")]
    public void GenerateTracks()
    {
        railPath = GetComponent<RailPath>();
        if (railPath == null || trackSegmentPrefab == null) return;

        // 1. Очистка
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Track_Container")) DestroyImmediate(child.gameObject);
        }

        GameObject rootContainer = new GameObject("Track_Container");
        rootContainer.transform.SetParent(transform);
        rootContainer.transform.localPosition = Vector3.zero;
        rootContainer.transform.localRotation = Quaternion.identity;
        rootContainer.transform.localScale = Vector3.one;

        // 2. Расчет
        railPath.RecalculatePath();
        float totalLen = railPath.TotalLength;
        int count = Mathf.RoundToInt(totalLen / segmentLength);
        if (count < 1) count = 1;

        float exactStep = totalLen / count;
        float stretchFactor = exactStep / segmentLength;

        // Подготавливаем вращение входного меша
        Quaternion meshRotFix = Quaternion.Euler(inputMeshRotation);

        // Словарь для материалов
        Dictionary<Material, List<CombineInstance>> materialCombines = new Dictionary<Material, List<CombineInstance>>();

        GameObject tempObj = Instantiate(trackSegmentPrefab);
        tempObj.transform.position = Vector3.zero;
        tempObj.transform.rotation = Quaternion.identity;
        tempObj.transform.localScale = Vector3.one;
        tempObj.SetActive(false);

        MeshFilter[] sourceFilters = tempObj.GetComponentsInChildren<MeshFilter>();

        int chunkID = 0;
        int currentSegmentCount = 0;

        // 3. Генерация
        for (int i = 0; i < count; i++)
        {
            float segmentStartDist = i * exactStep;

            // Центр сегмента
            Vector3 centerPos; Quaternion centerRot;
            railPath.GetPointAtDistance(segmentStartDist + (exactStep * 0.5f), out centerPos, out centerRot);
            centerPos += Vector3.up * heightOffset;

            foreach (var mf in sourceFilters)
            {
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                Material mat = mr.sharedMaterial;
                if (mat == null) continue;

                if (!materialCombines.ContainsKey(mat)) materialCombines[mat] = new List<CombineInstance>();

                Mesh meshToUse;
                Matrix4x4 childToRoot = tempObj.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;

                if (bendMeshesToSpline)
                {
                    // --- РЕЖИМ ИЗГИБА ---
                    meshToUse = Instantiate(mf.sharedMesh);
                    Vector3[] verts = meshToUse.vertices;

                    for (int v = 0; v < verts.Length; v++)
                    {
                        // 1. Получаем вершину в пространстве корня префаба
                        Vector3 vert = childToRoot.MultiplyPoint3x4(verts[v]);

                        // 2. Применяем КОРРЕКЦИЮ ВРАЩЕНИЯ (НОВОЕ!)
                        // Поворачиваем сами вершины, чтобы Y стал высотой, а Z длиной
                        vert = meshRotFix * vert;

                        // 3. Применяем ручной масштаб (теперь Y точно высота)
                        vert.x *= manualMeshScale.x;
                        vert.y *= manualMeshScale.y;
                        vert.z *= manualMeshScale.z;

                        // 4. Растягиваем длину (Z)
                        vert.z *= stretchFactor;

                        // 5. Гнем по сплайну
                        float distOnSpline = segmentStartDist + (exactStep * 0.5f) + vert.z;

                        if (railPath.loop) distOnSpline = Mathf.Repeat(distOnSpline, totalLen);
                        else distOnSpline = Mathf.Clamp(distOnSpline, 0, totalLen);

                        Vector3 splinePos; Quaternion splineRot;
                        railPath.GetPointAtDistance(distOnSpline, out splinePos, out splineRot);

                        Quaternion finalRot = splineRot * Quaternion.Euler(rotationOffset);

                        // Смещение X, Y (теперь Y точно смотрит вверх относительно рельсы)
                        Vector3 offset = finalRot * new Vector3(vert.x, vert.y, 0);

                        Vector3 worldPos = splinePos + offset;
                        worldPos += Vector3.up * heightOffset;

                        verts[v] = transform.InverseTransformPoint(worldPos);
                    }
                    meshToUse.vertices = verts;
                    meshToUse.RecalculateNormals();
                    meshToUse.RecalculateBounds();
                }
                else
                {
                    // ОБЫЧНЫЙ РЕЖИМ
                    meshToUse = mf.sharedMesh;
                }

                CombineInstance ci = new CombineInstance();
                ci.mesh = meshToUse;

                if (bendMeshesToSpline)
                {
                    ci.transform = Matrix4x4.identity;
                }
                else
                {
                    Quaternion finalRot = centerRot * Quaternion.Euler(rotationOffset);

                    // Учитываем вращение и масштаб
                    // Сначала скейл, потом поворот меша, потом childToRoot
                    Vector3 totalScale = manualMeshScale;
                    totalScale.z *= stretchFactor;

                    // Тут сложнее с матрицами, упростим для обычного режима:
                    // Просто используем childToRoot и накладываем сверху transform
                    // (Обычный режим редко требует такой точности как Bend)

                    // Для простоты в обычном режиме не применяем inputMeshRotation к вертексам,
                    // а крутим объект целиком
                    finalRot = finalRot * meshRotFix;

                    // Учет child offset... (упрощенно)
                    ci.transform = transform.worldToLocalMatrix * Matrix4x4.TRS(centerPos, finalRot, totalScale);
                }

                materialCombines[mat].Add(ci);
            }

            currentSegmentCount++;

            if (currentSegmentCount >= segmentsPerChunk || i == count - 1)
            {
                CreateMultiMaterialChunk(materialCombines, rootContainer.transform, chunkID++);
                foreach (var list in materialCombines.Values) list.Clear();
                currentSegmentCount = 0;
            }
        }

        DestroyImmediate(tempObj);
        Debug.Log("Генерация завершена!");
    }

    void CreateMultiMaterialChunk(Dictionary<Material, List<CombineInstance>> combinesMap, Transform parent, int id)
    {
        GameObject chunkRoot = new GameObject($"Chunk_{id}");
        chunkRoot.transform.SetParent(parent);
        chunkRoot.transform.localPosition = Vector3.zero;
        chunkRoot.transform.localRotation = Quaternion.identity;
        chunkRoot.transform.localScale = Vector3.one;
        chunkRoot.isStatic = true;

        foreach (var kvp in combinesMap)
        {
            Material mat = kvp.Key;
            List<CombineInstance> instances = kvp.Value;
            if (instances.Count == 0) continue;

            GameObject subChunk = new GameObject($"SubMesh_{mat.name}");
            subChunk.transform.SetParent(chunkRoot.transform);
            subChunk.transform.localPosition = Vector3.zero;
            subChunk.transform.localRotation = Quaternion.identity;
            subChunk.transform.localScale = Vector3.one;
            subChunk.isStatic = true;

            MeshFilter mf = subChunk.AddComponent<MeshFilter>();
            MeshRenderer mr = subChunk.AddComponent<MeshRenderer>();

            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(instances.ToArray(), true, true);

            mf.sharedMesh = combinedMesh;
            mr.sharedMaterial = mat;
        }
    }
}