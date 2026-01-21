using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class NavGridExporter : EditorWindow
{
    private const int MAGIC = 0x4E524744; // 'NRGD'
    private const int VERSION = 1;

    private static float cellSize = 0.5f;
    private static float sampleHeight = 2.0f;

    [MenuItem("Tools/Export/Export NavGrid (Binary)")]
    public static void ExportNavGrid()
    {
        // -----------------------------
        // 1. Scene Bounds 계산
        // -----------------------------
        var renderers = GameObject.FindObjectsOfType<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("Scene에 Renderer가 없습니다.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        Vector3 origin = new Vector3(
            bounds.min.x,
            bounds.min.y,
            bounds.min.z
        );

        int width = Mathf.CeilToInt(bounds.size.x / cellSize);
        int height = Mathf.CeilToInt(bounds.size.z / cellSize);

        Debug.Log($"[NavGrid] width={width}, height={height}, cellSize={cellSize}");
        Debug.Log($"[NavGrid] origin={origin}");

        // -----------------------------
        // 2. Grid Walkable 판정
        // -----------------------------
        bool[] walkables = new bool[width * height];

        for (int z = 0; z < height; ++z)
        {
            for (int x = 0; x < width; ++x)
            {
                Vector3 worldPos = new Vector3(
                    origin.x + (x + 0.5f) * cellSize,
                    origin.y + sampleHeight,
                    origin.z + (z + 0.5f) * cellSize
                );

                bool walkable = NavMesh.SamplePosition(
                    worldPos,
                    out NavMeshHit hit,
                    cellSize * 0.5f,
                    NavMesh.AllAreas
                );

                walkables[z * width + x] = walkable;
            }
        }

        // -----------------------------
        // 3. Binary Export
        // -----------------------------
        string exportDir = Application.dataPath + "/NavMeshExport";
        if (!Directory.Exists(exportDir))
            Directory.CreateDirectory(exportDir);

        string path = exportDir + "/navgrid.bin";

        using (BinaryWriter bw = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            // Header
            bw.Write(MAGIC);
            bw.Write(VERSION);

            // Grid Meta
            bw.Write(width);
            bw.Write(height);
            bw.Write(cellSize);

            bw.Write(origin.x);
            bw.Write(origin.y);
            bw.Write(origin.z);

            // Cell Data
            for (int i = 0; i < walkables.Length; ++i)
                bw.Write((byte)(walkables[i] ? 1 : 0));
        }

        Debug.Log($"NavGrid Export 완료: {path}");
        AssetDatabase.Refresh();
    }
}
