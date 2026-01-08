using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class NavmeshJsonExporter : EditorWindow
{
    [MenuItem("Tools/Export/Export NavMesh to JSON")]
    public static void ExportNavMeshToJson()
    {
        var tri = NavMesh.CalculateTriangulation();

        if (tri.vertices.Length == 0)
        {
            Debug.LogError("NavMesh is empty");
            return;
        }

        var path = Application.dataPath + "/NavMeshExport/navmesh.json";

        using (var writer = new StreamWriter(path))
        {
            writer.WriteLine("{");

            // vertices
            writer.WriteLine("\"vertices\":[");
            for (int i = 0; i < tri.vertices.Length; ++i)
            {
                var v = tri.vertices[i];
                writer.Write($"[{v.x},{v.y},{v.z}]");
                if (i + 1 < tri.vertices.Length) writer.Write(",");
            }
            writer.WriteLine("],");

            // indices
            writer.WriteLine("\"indices\":[");
            for (int i = 0; i < tri.indices.Length; ++i)
            {
                writer.Write(tri.indices[i]);
                if (i + 1 < tri.indices.Length) writer.Write(",");
            }
            writer.WriteLine("]");

            writer.WriteLine("}");
        }

        Debug.Log($"NavMesh JSON Exported: {path}");
        AssetDatabase.Refresh();
    }

}
