using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshExporter : EditorWindow
{
    private static string EXPORT_PATH = Application.dataPath + "/NavMeshExport/navmesh_collision.obj";

    [MenuItem("Tools/Export/Export NavMesh to OBJ")]
    public static void ExportNavMesh()
    {
        // 1. Unity NavMesh 데이터 추출
        // NavMesh 영역의 삼각형 분할(Triangulation) 정보를 가져옵니다.
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        if (navMeshData.vertices == null || navMeshData.vertices.Length == 0)
        {
            Debug.LogError("현재 씬에 유효한 NavMesh 데이터가 없습니다.");
            return;
        }

        // 2. OBJ 파일 포맷으로 변환 시작
        StringBuilder objBuilder = new StringBuilder();

        // 2-A. 주석 및 헤더
        objBuilder.AppendLine("# Exported NavMesh data from Unity to OBJ format");
        objBuilder.AppendLine($"# Vertices: {navMeshData.vertices.Length}, Faces: {navMeshData.indices.Length / 3}");

        // 2-B. 정점 (v) 정보 기록
        // 정점 위치(Vector3)를 OBJ 포맷에 맞게 기록합니다.
        foreach (Vector3 vertex in navMeshData.vertices)
        {
            // Unity의 좌표계를 OBJ 파일에 맞게 변환할 필요가 있다면 여기서 처리합니다.
            // 기본적으로 X Y Z 순으로 기록합니다.
            objBuilder.AppendLine($"v {vertex.x:F6} {vertex.y:F6} {vertex.z:F6}");
        }

        // 2-C. 면 (f) 정보 기록
        // 삼각형(3개의 인덱스)을 순회하며 Face 정보를 기록합니다.
        // **중요**: OBJ 파일의 인덱스는 1부터 시작합니다. (Unity는 0부터 시작)
        for (int i = 0; i < navMeshData.indices.Length; i += 3)
        {
            // 인덱스 값에 1을 더해 OBJ 포맷에 맞춥니다.
            int v1 = navMeshData.indices[i] + 1;
            int v2 = navMeshData.indices[i + 1] + 1;
            int v3 = navMeshData.indices[i + 2] + 1;

            objBuilder.AppendLine($"f {v1} {v2} {v3}");
        }

        // 3. 파일 저장
        string directoryPath = Path.GetDirectoryName(EXPORT_PATH);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(EXPORT_PATH, objBuilder.ToString());

        Debug.Log($"OBJ 파일 추출 완료! 경로: {EXPORT_PATH}");
        AssetDatabase.Refresh();
    }
}