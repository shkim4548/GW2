using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.IO;

public class NavGridBinExporterEditor : EditorWindow
{
    private const uint MAGIC = 0x4E524744; // 'NRGD'
    private const uint VERSION = 1;

    // ===== User Editable Parameters =====
    private float cellSize = 0.5f;
    private float rayHeightPadding = 1.0f;
    private float rayDistance = 100f;
    private float navmeshSampleRadius = 0.1f;

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

        if (GUILayout.Button("Export NavGrid (Binary)"))
        {
            Export();
        }
    }

    private void Export()
    {
        Bounds bounds = CalculateWorldBounds();

        int width = Mathf.CeilToInt(bounds.size.x / cellSize);
        int height = Mathf.CeilToInt(bounds.size.z / cellSize);

        Vector3 origin = new Vector3(
            bounds.min.x,
            bounds.min.y,
            bounds.min.z
        );

        bool[] walkables = new bool[width * height];
        float[] heights = new float[width * height];

        for (int z = 0; z < height; ++z)
        {
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
            }
        }

        WriteBinary(width, height, cellSize, origin, walkables, heights);
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
                bw.Write(heights[i]);
            }
        }

        Debug.Log($"[NavGrid Exported] {width}x{height}, Walkable={CountWalkables(walkables)}");
        AssetDatabase.Refresh();
    }

    private static Bounds CalculateWorldBounds()
    {
        Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }

    private static int CountWalkables(bool[] w)
    {
        int c = 0;
        foreach (var b in w)
            if (b) c++;
        return c;
    }
}