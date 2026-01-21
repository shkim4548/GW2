using System.IO;
using UnityEngine;
using UnityEditor;
using Data;

public static class NavGridBinExporter
{
    private const string EXPORT_PATH =
        "Assets/NavMeshExport/navgrid.bin";

    [MenuItem("Tools/Export/Export NavGrid (BIN)")]
    public static void ExportNavGridBin()
    {
        // 1. Grid 생성
        Bounds bounds = CalculateWorldBounds();

        float cellSize = 0.5f;

        int width, height;
        Vector3 origin;

        GridCell[] cells = NavGridBuilder.BuildNavGrid(
            bounds,
            cellSize,
            out width,
            out height,
            out origin
        );

        // 2. 파일 열기
        using (FileStream fs = new FileStream(EXPORT_PATH, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            // Header
            bw.Write((uint)0x4E524744); // 'NRGD'
            bw.Write((uint)1);          // version

            // Meta
            bw.Write(width);
            bw.Write(height);
            bw.Write(cellSize);

            bw.Write(origin.x);
            bw.Write(origin.y);
            bw.Write(origin.z);

            // Cells
            foreach (var c in cells)
            {
                bw.Write(c.walkable);
                bw.Write(c.height);
                bw.Write(c.n0);
                bw.Write(c.n1);
                bw.Write(c.n2);
                bw.Write(c.n3);
                bw.Write(c.x);
                bw.Write(c.z);
            }
        }

        Debug.Log(
            $"[NavGrid Exported] {width}x{height} = {cells.Length} cells"
        );

        AssetDatabase.Refresh();
    }

    // 씬 전체 Bounds 계산 (간단 버전)
    private static Bounds CalculateWorldBounds()
    {
        Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        return bounds;
    }
}
