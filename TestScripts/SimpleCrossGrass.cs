using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SimpleCrossGrass : MonoBehaviour
{
    [System.Serializable]
    public class GrassLayer
    {
        public string name = "New Layer";
        public Material material;

        [Header("Дальность Видимости")]
        public float maxDistance = 100f; // Где трава исчезает полностью
        public float fadeRange = 20f;    // На каком расстоянии до конца начинает исчезать

        [Header("Размеры")]
        public Vector2 sizeRange = new Vector2(0.8f, 1.2f);
        public float width = 1.0f;
        public float height = 1.0f;

        [Header("Цвет")]
        public Color topColor = new Color(1f, 1f, 0.8f);
        public Color bottomColor = new Color(0.2f, 0.5f, 0.2f);
        [Range(0, 1)] public float randomDarken = 0.3f;

        [Header("Ветер")]
        public float windSpeed = 2.0f;
        public float windStrength = 0.2f;

        [Header("Тени")]
        public bool castShadows = true;
        public bool receiveShadows = true;

        [HideInInspector] public List<Matrix4x4> matrices = new List<Matrix4x4>();
        [HideInInspector] public List<GrassBatch> batches = new List<GrassBatch>();
        [HideInInspector] public bool needsRebatching = true;
    }

    [System.Serializable]
    public class GrassBatch
    {
        public List<Matrix4x4> matrices;
        public Bounds bounds;
        public Vector3 center; // Центр для проверки дистанции
    }

    [Header("Глобальные настройки")]
    public LayerMask paintLayer = 1;
    [Range(0, 1)] public float blackRemoval = 0.1f;

    public List<GrassLayer> layers = new List<GrassLayer>();
    [HideInInspector] public int selectedLayerIndex = 0;

    private Mesh crossMesh;
    private const int BATCH_SIZE = 100;
    private Vector4[] interactorPositions = new Vector4[10];

    private void OnEnable()
    {
        if (crossMesh == null) GenerateCrossMesh();
        foreach (var layer in layers) layer.needsRebatching = true;
    }

    private void OnValidate()
    {
        foreach (var layer in layers) layer.needsRebatching = true;
#if UNITY_EDITOR
        EditorApplication.QueuePlayerLoopUpdate();
#endif
    }

    private void Update()
    {
        UpdateInteractors();

        if (crossMesh == null) GenerateCrossMesh();
        if (layers.Count == 0) return;

        Camera cam = GetCamera();
        if (cam == null) return;
        Vector3 camPos = cam.transform.position;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

        foreach (var layer in layers)
        {
            if (layer.material == null || layer.matrices.Count == 0) continue;

            if (layer.needsRebatching) RebuildBatches(layer);

            // Передаем параметры в шейдер
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetFloat("_Cutoff", blackRemoval);
            properties.SetFloat("_WindSpeed", layer.windSpeed);
            properties.SetFloat("_WindStrength", layer.windStrength);
            properties.SetColor("_TopColor", layer.topColor);
            properties.SetColor("_BottomColor", layer.bottomColor);
            properties.SetFloat("_RandomDarken", layer.randomDarken);

            // --- ПЕРЕДАЕМ ДИСТАНЦИЮ В ШЕЙДЕР ---
            properties.SetFloat("_MaxDist", layer.maxDistance);
            properties.SetFloat("_FadeRange", layer.fadeRange);
            // -----------------------------------

            UnityEngine.Rendering.ShadowCastingMode shadowMode = layer.castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;

            foreach (var batch in layer.batches)
            {
                // 1. Проверка Дистанции (оптимизация CPU)
                // Если вся пачка дальше, чем макс. дистанция - даже не пытаемся рисовать
                float distToBatch = Vector3.Distance(camPos, batch.center);
                // Добавляем запас (radius), чтобы не исчезало раньше времени
                if (distToBatch > layer.maxDistance + 20.0f) continue;

                // 2. Проверка Видимости (Frustum Culling)
                if (GeometryUtility.TestPlanesAABB(planes, batch.bounds))
                {
                    Graphics.DrawMeshInstanced(
                        crossMesh,
                        0,
                        layer.material,
                        batch.matrices,
                        properties,
                        shadowMode,
                        layer.receiveShadows,
                        gameObject.layer
                    );
                }
            }
        }
    }

    void UpdateInteractors()
    {
        int count = 0;
        if (GrassInteractor.AllInteractors != null)
        {
            count = GrassInteractor.AllInteractors.Count;
            for (int i = 0; i < interactorPositions.Length; i++)
            {
                if (i < count && GrassInteractor.AllInteractors[i] != null)
                {
                    var inter = GrassInteractor.AllInteractors[i];
                    interactorPositions[i] = new Vector4(inter.transform.position.x, inter.transform.position.y, inter.transform.position.z, inter.radius);
                }
                else
                {
                    interactorPositions[i] = Vector4.zero;
                }
            }
        }
        Shader.SetGlobalVectorArray("_Interactors", interactorPositions);
        Shader.SetGlobalFloat("_InteractorsCount", count);
    }

    public void RebuildBatches(GrassLayer layer)
    {
        layer.batches.Clear();
        layer.matrices.Sort((a, b) => {
            int zDiff = a.m23.CompareTo(b.m23);
            if (zDiff != 0) return zDiff;
            return a.m03.CompareTo(b.m03);
        });

        for (int i = 0; i < layer.matrices.Count; i += BATCH_SIZE)
        {
            int count = Mathf.Min(BATCH_SIZE, layer.matrices.Count - i);
            List<Matrix4x4> batchList = layer.matrices.GetRange(i, count);

            if (batchList.Count > 0)
            {
                Bounds b = new Bounds(batchList[0].GetColumn(3), Vector3.one);
                Vector3 centerSum = Vector3.zero;

                for (int j = 0; j < batchList.Count; j++)
                {
                    Vector3 pos = batchList[j].GetColumn(3);
                    b.Encapsulate(pos);
                    centerSum += pos;
                }

                float maxSize = Mathf.Max(layer.width, layer.height) * 2f;
                b.Expand(maxSize);

                // Сохраняем центр пачки для быстрой проверки дистанции
                Vector3 batchCenter = centerSum / batchList.Count;

                layer.batches.Add(new GrassBatch { matrices = batchList, bounds = b, center = batchCenter });
            }
        }
        layer.needsRebatching = false;
    }

    Camera GetCamera()
    {
        if (Application.isPlaying) return Camera.main;
#if UNITY_EDITOR
        return SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera : null;
#else
        return null;
#endif
    }

    public void SetDirty(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < layers.Count)
            layers[layerIndex].needsRebatching = true;
    }

    void GenerateCrossMesh()
    {
        crossMesh = new Mesh();
        crossMesh.name = "ProceduralCrossQuad";
        float w = 0.5f; float h = 1.0f;
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-w, 0, 0), new Vector3(w, 0, 0), new Vector3(-w, h, 0), new Vector3(w, h, 0),
            new Vector3(0, 0, -w), new Vector3(0, 0, w), new Vector3(0, h, -w), new Vector3(0, h, w)
        };
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
        };
        Vector3 n1 = Vector3.forward; Vector3 n2 = Vector3.right;
        Vector3[] normals = new Vector3[] { n1, n1, n1, n1, n2, n2, n2, n2 };
        int[] triangles = new int[] { 0, 2, 1, 2, 3, 1, 4, 6, 5, 6, 7, 5 };
        crossMesh.vertices = vertices; crossMesh.uv = uvs; crossMesh.normals = normals; crossMesh.triangles = triangles;
        crossMesh.RecalculateBounds();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SimpleCrossGrass))]
public class SimpleCrossGrassEditor : Editor
{
    float brushRadius = 2.0f;
    int brushDensity = 5;
    bool eraseAllLayers = false;
    SerializedProperty layersProp;
    SerializedProperty paintLayerProp;
    SerializedProperty blackRemovalProp;

    void OnEnable()
    {
        layersProp = serializedObject.FindProperty("layers");
        paintLayerProp = serializedObject.FindProperty("paintLayer");
        blackRemovalProp = serializedObject.FindProperty("blackRemoval");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SimpleCrossGrass script = (SimpleCrossGrass)target;

        EditorGUILayout.PropertyField(paintLayerProp);
        EditorGUILayout.PropertyField(blackRemovalProp);

        GUILayout.Space(10);
        GUILayout.Label("Управление Слоями", EditorStyles.boldLabel);

        if (GUILayout.Button("+ Добавить Слой"))
        {
            Undo.RecordObject(script, "Add Layer");
            script.layers.Add(new SimpleCrossGrass.GrassLayer());
            script.selectedLayerIndex = script.layers.Count - 1;
            serializedObject.Update();
        }

        if (script.layers.Count > 0)
        {
            string[] layerNames = script.layers.Select((l, i) => $"{i + 1}: {l.name} ({l.matrices.Count})").ToArray();
            script.selectedLayerIndex = EditorGUILayout.Popup("Активный Слой", script.selectedLayerIndex, layerNames);

            if (GUILayout.Button("Удалить Слой"))
            {
                if (EditorUtility.DisplayDialog("Удаление", "Удалить слой?", "Да", "Нет"))
                {
                    script.layers.RemoveAt(script.selectedLayerIndex);
                    script.selectedLayerIndex = Mathf.Max(0, script.layers.Count - 1);
                    return;
                }
            }

            GUILayout.Space(10);
            if (script.selectedLayerIndex < layersProp.arraySize)
            {
                SerializedProperty activeLayerProp = layersProp.GetArrayElementAtIndex(script.selectedLayerIndex);
                GUILayout.BeginVertical("box");
                GUILayout.Label($"Настройки Слоя {script.selectedLayerIndex + 1}", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("name"));
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("material"));

                GUILayout.Space(5);
                // Добавленные поля для дальности
                EditorGUILayout.LabelField("Видимость", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("maxDistance"));
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("fadeRange"));

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Геометрия", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("sizeRange"));
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("width"));
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("height"));

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Цвет", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("topColor"));
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("bottomColor"));
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("randomDarken"));

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Ветер", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("windSpeed"));
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("windStrength"));

                GUILayout.Space(5);
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("castShadows"));
                EditorGUILayout.PropertyField(activeLayerProp.FindPropertyRelative("receiveShadows"));
                GUILayout.EndVertical();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Нет слоев. Добавьте слой!", MessageType.Info);
        }

        GUILayout.Space(20);
        GUILayout.Label("Кисть (Shift = +, Ctrl = -)", EditorStyles.boldLabel);
        brushRadius = EditorGUILayout.Slider("Радиус", brushRadius, 0.1f, 10f);
        brushDensity = EditorGUILayout.IntSlider("Плотность", brushDensity, 1, 20);
        eraseAllLayers = EditorGUILayout.Toggle("Ластик стирает ВСЁ", eraseAllLayers);

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        SimpleCrossGrass script = (SimpleCrossGrass)target;
        if (script.layers.Count == 0) return;

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, script.paintLayer))
        {
            Handles.color = new Color(0, 1, 0, 0.5f);
            Handles.DrawWireDisc(hit.point, hit.normal, brushRadius);
            HandleUtility.Repaint();

            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && e.modifiers == EventModifiers.Shift)
            {
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                for (int i = 0; i < brushDensity; i++)
                    AddGrass(script, hit.point, script.selectedLayerIndex);
                e.Use();
            }

            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && e.modifiers == EventModifiers.Control)
            {
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                RemoveGrass(script, hit.point);
                e.Use();
            }
        }
        if (e.type == EventType.MouseUp) GUIUtility.hotControl = 0;
    }

    void AddGrass(SimpleCrossGrass script, Vector3 center, int layerIndex)
    {
        var layer = script.layers[layerIndex];
        Vector2 rnd = Random.insideUnitCircle * brushRadius;
        Vector3 pos = center + new Vector3(rnd.x, 0, rnd.y);

        if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, script.paintLayer))
        {
            Undo.RecordObject(script, "Paint Grass");
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
            float scale = Random.Range(layer.sizeRange.x, layer.sizeRange.y);
            Vector3 scaleVec = new Vector3(layer.width * scale, layer.height * scale, layer.width * scale);
            Matrix4x4 m = Matrix4x4.TRS(hit.point, rot, scaleVec);
            layer.matrices.Add(m);
            script.SetDirty(layerIndex);
        }
    }

    void RemoveGrass(SimpleCrossGrass script, Vector3 center)
    {
        Undo.RecordObject(script, "Remove Grass");
        List<int> layersToProcess = new List<int>();
        if (eraseAllLayers)
            layersToProcess = Enumerable.Range(0, script.layers.Count).ToList();
        else
            layersToProcess.Add(script.selectedLayerIndex);

        foreach (int idx in layersToProcess)
        {
            var layer = script.layers[idx];
            int removedCount = layer.matrices.RemoveAll(m => Vector3.Distance(m.GetColumn(3), center) < brushRadius);
            if (removedCount > 0) script.SetDirty(idx);
        }
    }
}
#endif