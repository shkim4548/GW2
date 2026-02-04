using Codice.Client.BaseCommands;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static Codice.Client.Common.WebApi.WebApiEndpoints;

public class NavGridBinExporterEditor : EditorWindow
{
    private const uint MAGIC = 0x4E524744;
    private const uint VERSION = 1;

    private float cellSize = 0.5f;
    private float rayHeightPadding = 1.0f;
    private float rayDistance = 100f;
    private float navmeshSampleRadius = 0.1f;

    // ===== 추가: 맵 범위 수동 지정 =====
    private GameObject mapRoot;  // 맵 부모 오브젝트
    private bool useManualBounds = false;
    private Vector3 manualBoundsMin;
    private Vector3 manualBoundsMax;

    [MenuItem("Tools/NavGrid/Export NavGrid (BIN)")]
    public static void Open()
    {
        GetWindow<NavGridBinExporterEditor>("NavGrid Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("NavGrid Export Settings", EditorStyles.boldLabel);

        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        rayHeightPadding = EditorGUILayout.FloatField("Ray Height Padding", rayHeightPadding);
        rayDistance = EditorGUILayout.FloatField("Ray Distance", rayDistance);
        navmeshSampleRadius = EditorGUILayout.FloatField("NavMesh Sample Radius", navmeshSampleRadius);

        GUILayout.Space(10);
        GUILayout.Label("Bounds Settings", EditorStyles.boldLabel);

        // ===== 옵션 1: Map Root 지정 =====
        mapRoot = (GameObject)EditorGUILayout.ObjectField(
            "Map Root (Optional)",
            mapRoot,
            typeof(GameObject),
            true
        );

        // ===== 옵션 2: 수동 범위 지정 =====
        useManualBounds = EditorGUILayout.Toggle("Use Manual Bounds", useManualBounds);

        if (useManualBounds)
        {
            manualBoundsMin = EditorGUILayout.Vector3Field("Min", manualBoundsMin);
            manualBoundsMax = EditorGUILayout.Vector3Field("Max", manualBoundsMax);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Export NavGrid (Binary)"))
        {
            Export();
        }

        // ===== 미리보기 버튼 추가 =====
        if (GUILayout.Button("Preview Bounds (Gizmo)"))
        {
            PreviewBounds();
        }
    }

    private void Export()
    {
        Bounds bounds = CalculateWorldBounds();

        // ===== 안전장치 추가 =====
        int width = Mathf.CeilToInt(bounds.size.x / cellSize);
        int height = Mathf.CeilToInt(bounds.size.z / cellSize);

        if (width * height > 1000000)  // 100만 셀 이상
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Warning: Large Grid",
                $"Grid size is {width} x {height} = {width * height} cells.\n" +
                $"This may take very long time and consume lots of memory.\n" +
                $"Continue?",
                "Yes", "Cancel"
            );

            if (!confirm) return;
        }

        Vector3 origin = new Vector3(
            bounds.min.x,
            bounds.min.y,
            bounds.min.z
        );

        bool[] walkables = new bool[width * height];
        float[] heights = new float[width * height];

        // ===== 진행도 표시 추가 =====
        int totalCells = width * height;
        int processedCells = 0;

        for (int z = 0; z < height; ++z)
        {
            // 진행도 표시 (100행마다)
            if (z % 100 == 0)
            {
                float progress = (float)processedCells / totalCells;
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Exporting NavGrid",
                    $"Processing row {z}/{height}",
                    progress))
                {
                    EditorUtility.ClearProgressBar();
                    Debug.Log("Export cancelled by user");
                    return;
                }
            }

            for (int x = 0; x < width; ++x)
            {
                Vector3 rayStart = new Vector3(
                    origin.x + (x + 0.5f) * cellSize,
                    bounds.max.y + rayHeightPadding,
                    origin.z + (z + 0.5f) * cellSize
                );

                int idx = z * width + x;

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance))
                {
                    bool ok = NavMesh.SamplePosition(
                        hit.point,
                        out NavMeshHit navHit,
                        navmeshSampleRadius,
                        NavMesh.AllAreas
                    );

                    walkables[idx] = ok;
                    heights[idx] = ok ? navHit.position.y : 0f;
                }
                else
                {
                    walkables[idx] = false;
                    heights[idx] = 0f;
                }

                processedCells++;
            }
        }

        EditorUtility.ClearProgressBar();

        WriteBinary(width, height, cellSize, origin, walkables, heights);
    }

    private Bounds CalculateWorldBounds()
    {
        // ===== 옵션 1: 수동 범위 =====
        if (useManualBounds)
        {
            Vector3 center = (manualBoundsMin + manualBoundsMax) / 2f;
            Vector3 size = manualBoundsMax - manualBoundsMin;
            return new Bounds(center, size);
        }

        // ===== 옵션 2: Map Root 지정 =====
        if (mapRoot != null)
        {
            Renderer[] renderers = mapRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError("No renderers found in Map Root!");
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            Debug.Log($"[Bounds from Map Root] Size: {bounds.size}, Center: {bounds.center}");
            return bounds;
        }

        // ===== 옵션 3: NavMesh 기준 (기본) =====
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.vertices.Length == 0)
        {
            Debug.LogError("No NavMesh found! Please bake NavMesh first.");
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds navBounds = new Bounds(triangulation.vertices[0], Vector3.zero);
        foreach (var v in triangulation.vertices)
            navBounds.Encapsulate(v);

        Debug.Log($"[Bounds from NavMesh] Size: {navBounds.size}, Center: {navBounds.center}");
        return navBounds;
    }

    private void PreviewBounds()
    {
        Bounds bounds = CalculateWorldBounds();

        Debug.Log($"=== NavGrid Bounds Preview ===");
        Debug.Log($"Min: {bounds.min}");
        Debug.Log($"Max: {bounds.max}");
        Debug.Log($"Size: {bounds.size}");
        Debug.Log($"Grid Size: {Mathf.CeilToInt(bounds.size.x / cellSize)} x {Mathf.CeilToInt(bounds.size.z / cellSize)}");

        // Scene View에 Gizmo 그리기
        Selection.activeObject = null;
        SceneView.lastActiveSceneView.Frame(bounds, false);
    }

    private void WriteBinary(
        int width,
        int height,
        float cellSize,
        Vector3 origin,
        bool[] walkables,
        float[] heights)
    {
        string dir = "Assets/NavMeshExport";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = $"{dir}/navgrid.bin";

        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(MAGIC);
            bw.Write(VERSION);

            bw.Write(width);
            bw.Write(height);
            bw.Write(cellSize);

            bw.Write(origin.x);
            bw.Write(origin.y);
            bw.Write(origin.z);

            for (int i = 0; i < walkables.Length; ++i)
            {
                bw.Write((byte)(walkables[i] ? 1 : 0));
            }

            // ===== heights는 제거 (서버에서 안 씀) =====
            // for (int i = 0; i < heights.Length; ++i)
            // {
            //     bw.Write(heights[i]);
            // }
        }

        int walkableCount = CountWalkables(walkables);
        float walkablePercent = (float)walkableCount / walkables.Length * 100f;

        Debug.Log($"[NavGrid Exported]");
        Debug.Log($"  Size: {width} x {height} = {width * height} cells");
        Debug.Log($"  Walkable: {walkableCount} ({walkablePercent:F1}%)");
        Debug.Log($"  File: {path}");

        AssetDatabase.Refresh();
    }

    private static int CountWalkables(bool[] w)
    {
        int c = 0;
        foreach (var b in w)
            if (b) c++;
        return c;
    }
}