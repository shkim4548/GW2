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
            BakeGrid();
    }

    void BakeGrid()
    {
        var triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices.Length == 0)
        {
            Debug.LogError("NavMesh not baked.");
            return;
        }

        // Bounds 계산
        Vector3 min = triangulation.vertices[0];
        Vector3 max = triangulation.vertices[0];

        foreach (var v in triangulation.vertices)
        {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        int width = Mathf.CeilToInt((max.x - min.x) / cellSize);
        int height = Mathf.CeilToInt((max.z - min.z) / cellSize);

        NavGridData grid = new NavGridData();
        grid.header = new NavGridHeader
        {
            magic = 0x4E524744, // 'NRGD'
            version = 1,
            width = width,
            height = height,
            cellSize = cellSize,
            origin = min
        };

        grid.cells = new GridCell[width * height];

        // 1단계: walkable + height
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = z * width + x;

                Vector3 worldPos = new Vector3(
                    min.x + (x + 0.5f) * cellSize,
                    min.y + sampleHeight,
                    min.z + (z + 0.5f) * cellSize
                );

                if (NavMesh.SamplePosition(
                        worldPos,
                        out NavMeshHit hit,
                        sampleHeight * 2,
                        NavMesh.AllAreas))
                {
                    grid.cells[idx].flags |= 1;          // walkable
                    grid.cells[idx].height = hit.position.y;
                }
            }
        }

        // 2단계: neighbor links 계산
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = z * width + x;
                if ((grid.cells[idx].flags & 1) == 0)
                    continue;

                TryLink(grid, x, z, x, z + 1, Define.Dir.North);
                TryLink(grid, x, z, x + 1, z, Define.Dir.East);
                TryLink(grid, x, z, x, z - 1, Define.Dir.South);
                TryLink(grid, x, z, x - 1, z, Define.Dir.West);
            }
        }

        SaveBinary(grid);
        Debug.Log("NavGrid bake completed.");
    }

    void TryLink(NavGridData grid, int x, int z, int nx, int nz, Define.Dir dir)
    {
        int w = grid.header.width;
        int h = grid.header.height;

        if (nx < 0 || nz < 0 || nx >= w || nz >= h)
            return;

        int fromIdx = z * w + x;
        int toIdx = nz * w + nx;

        if ((grid.cells[toIdx].flags & 1) == 0)
            return;

        float dh = Mathf.Abs(grid.cells[fromIdx].height - grid.cells[toIdx].height);
        if (dh > 2.0f) // RTS 기준 허용 높이차
            return;

        grid.cells[fromIdx].links |= (byte)(1 << (int)dir);
    }

    void SaveBinary(NavGridData grid)
    {
        string path = Application.dataPath + "/NavMeshExport/navgrid.bin";
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        using (BinaryWriter bw = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            bw.Write(grid.header.magic);
            bw.Write(grid.header.version);
            bw.Write(grid.header.width);
            bw.Write(grid.header.height);
            bw.Write(grid.header.cellSize);
            bw.Write(grid.header.origin.x);
            bw.Write(grid.header.origin.y);
            bw.Write(grid.header.origin.z);

            foreach (var c in grid.cells)
            {
                bw.Write(c.flags);
                bw.Write(c.links);
                bw.Write(c.height);
            }
        }

        AssetDatabase.Refresh();
    }
}
