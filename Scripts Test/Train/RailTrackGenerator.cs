using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RailPath))]
public class RailTrackGenerator : MonoBehaviour
{
    [Header("Ресурсы")]
    public GameObject trackSegmentPrefab;

    [Header("Настройки")]
    [Tooltip("Длина модели рельсы.")]
    public float segmentLength = 1.0f;
    public float heightOffset = 0.0f;

    [Header("ВАЖНО: Ориентация")]
    public Vector3 inputMeshRotation = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Исправление Размера")]
    public Vector3 manualMeshScale = Vector3.one;

    [Header("Искривление")]
    public bool bendMeshesToSpline = true;

    [Header("Оптимизация")]
    public bool useOptimization = true;
    public int segmentsPerChunk = 50;

    private RailPath railPath;

    // --- ГЛАВНАЯ КНОПКА ГЕНЕРАЦИИ (Включая ветки) ---
    [ContextMenu("Generate ALL Tracks (Recursive)")]
    public void GenerateAllTracks()
    {
        // 1. Генерируем текущий путь
        GenerateTracksLocal();

        // 2. Ищем ветки в RailPath
        if (railPath == null) railPath = GetComponent<RailPath>();

        // Убедимся, что путь пересчитан и знает о своих ветках
        railPath.RecalculatePath();

        foreach (var branch in railPath.childBranches)
        {
            // Пытаемся найти генератор на ветке
            RailTrackGenerator branchGen = branch.GetComponent<RailTrackGenerator>();

            // Если генератора нет на ветке, добавляем его и копируем настройки с "Мамы"
            if (branchGen == null)
            {
                branchGen = branch.gameObject.AddComponent<RailTrackGenerator>();
                CopySettings(this, branchGen);
            }

            // Рекурсивный вызов
            branchGen.GenerateAllTracks();
        }
    }

    // Копирование настроек для новых веток
    void CopySettings(RailTrackGenerator source, RailTrackGenerator target)
    {
        target.trackSegmentPrefab = source.trackSegmentPrefab;
        target.segmentLength = source.segmentLength;
        target.heightOffset = source.heightOffset;
        target.inputMeshRotation = source.inputMeshRotation;
        target.rotationOffset = source.rotationOffset;
        target.manualMeshScale = source.manualMeshScale;
        target.bendMeshesToSpline = source.bendMeshesToSpline;
        target.useOptimization = source.useOptimization;
        target.segmentsPerChunk = source.segmentsPerChunk;
    }


    // --- Локальная генерация (для одного сплайна) ---
    [ContextMenu("Generate Tracks (Local Only)")]
    public void GenerateTracksLocal()
    {
        railPath = GetComponent<RailPath>();
        if (railPath == null || trackSegmentPrefab == null) return;

        // 1. Очистка старых контейнеров
        var existingContainer = transform.Find("Track_Container");
        if (existingContainer != null) DestroyImmediate(existingContainer.gameObject);

        GameObject rootContainer = new GameObject("Track_Container");
        rootContainer.transform.SetParent(transform);
        rootContainer.transform.localPosition = Vector3.zero;
        rootContainer.transform.localRotation = Quaternion.identity;
        rootContainer.transform.localScale = Vector3.one;

        // 2. Расчет
        // railPath.RecalculatePath(); // Убрали, чтобы не сбивать рекурсию, расчет вызывается снаружи
        float totalLen = railPath.TotalLength;

        // Если длина 0 (например, у ветки только 1 точка и она равна родителю), пропускаем
        if (totalLen < 0.01f) return;

        int count = Mathf.RoundToInt(totalLen / segmentLength);
        if (count < 1) count = 1;

        float exactStep = totalLen / count;
        float stretchFactor = exactStep / segmentLength;

        Quaternion meshRotFix = Quaternion.Euler(inputMeshRotation);
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
                    meshToUse = Instantiate(mf.sharedMesh);
                    Vector3[] verts = meshToUse.vertices;

                    for (int v = 0; v < verts.Length; v++)
                    {
                        Vector3 vert = childToRoot.MultiplyPoint3x4(verts[v]);
                        vert = meshRotFix * vert;
                        vert.x *= manualMeshScale.x;
                        vert.y *= manualMeshScale.y;
                        vert.z *= manualMeshScale.z;
                        vert.z *= stretchFactor;

                        float distOnSpline = segmentStartDist + (exactStep * 0.5f) + vert.z;

                        if (railPath.loop) distOnSpline = Mathf.Repeat(distOnSpline, totalLen);
                        else distOnSpline = Mathf.Clamp(distOnSpline, 0, totalLen);

                        Vector3 splinePos; Quaternion splineRot;
                        railPath.GetPointAtDistance(distOnSpline, out splinePos, out splineRot);

                        Quaternion finalRot = splineRot * Quaternion.Euler(rotationOffset);
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
                    Vector3 totalScale = manualMeshScale;
                    totalScale.z *= stretchFactor;
                    finalRot = finalRot * meshRotFix;
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