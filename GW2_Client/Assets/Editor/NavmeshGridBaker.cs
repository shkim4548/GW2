using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using Data;

public class NavMeshGridBaker : EditorWindow
{
    float cellSize = 0.5f;
    float sampleHeight = 5.0f;

    [MenuItem("Tools/NavMesh/Bake Grid")]
    static void Open()
    {
        GetWindow<NavMeshGridBaker>("NavMesh Grid Baker");
    }

    void OnGUI()
    {
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        sampleHeight = EditorGUILayout.FloatField("Sample Height", sampleHeight);

        if (GUILayout.Button("Bake Grid"))
        {
            BakeGrid();
        }
    }

    void BakeGrid()
    {
        var triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices.Length == 0)
        {
            Debug.LogError("NavMesh not baked.");
            return;
        }

        // 1. Bounds °è»ê
        Vector3 min = triangulation.vertices[0];
        Vector3 max = triangulation.vertices[0];

        foreach (var v in triangulation.vertices)
        {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        int width = Mathf.CeilToInt((max.x - min.x) / cellSize);
        int height = Mathf.CeilToInt((max.z - min.z) / cellSize);

        Debug.Log($"Grid Size: {width} x {height}");

        NavGridData grid = new NavGridData
        {
            width = width,
            height = height,
            cellSize = cellSize,
            origin = min,
            cells = new GridCell[width * height]
        };

        // 2. Grid »ùÇÃ¸µ
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 worldPos = new Vector3(
                    min.x + x * cellSize + cellSize * 0.5f,
                    min.y + sampleHeight,
                    min.z + z * cellSize + cellSize * 0.5f
                );

                int idx = z * width + x;

                if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, sampleHeight * 2, NavMesh.AllAreas))
                {
                    grid.cells[idx].walkable = true;
                    grid.cells[idx].height = hit.position.y;
                }
                else
                {
                    grid.cells[idx].walkable = false;
                    grid.cells[idx].height = 0;
                }
            }
        }

        SaveBinary(grid);
        Debug.Log("NavGrid bake completed.");
    }

    void SaveBinary(NavGridData grid)
    {
        string path = Application.dataPath + "/NavMeshExport/navgrid.bin";
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        using (BinaryWriter bw = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            // ===== FileHeader =====
            bw.Write(0x4E524744); // 'NRGD'
            bw.Write((ushort)1); // version

            // ===== Grid Meta =====
            bw.Write(grid.width);
            bw.Write(grid.height);
            bw.Write(grid.cellSize);
            bw.Write(grid.origin.x);
            bw.Write(grid.origin.y);
            bw.Write(grid.origin.z);

            // ===== Cells =====
            foreach (var c in grid.cells)
            {
                bw.Write(c.walkable);
                bw.Write(c.height);
            }
        }

        Debug.Log($"Saved: {path}");
        AssetDatabase.Refresh();
    }

}
