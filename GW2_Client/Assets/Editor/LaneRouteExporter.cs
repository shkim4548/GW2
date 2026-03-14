#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Data;

public class LaneRouteExporter : EditorWindow
{
    [Header("Output")]
    private string fileName = "laneRoutes.json";

    // EditorWindow 필드 추가
    public Transform topLaneRoot;   // 씬에서 드래그
    public Transform botLaneRoot;   // 씬에서 드래그
    public int topLaneId = 1;
    public int botLaneId = 3;

    [MenuItem("Tools/Lane/Lane Route Exporter (JSON)")]
    public static void ShowWindow()
    {
        GetWindow<LaneRouteExporter>("Lane Route Exporter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Lane Route Exporter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "씬에 배치된 LaneRouteRoot + LaneWaypoint 컴포넌트를 수집하여 laneRoutes.json으로 export합니다.",
            MessageType.Info);

        EditorGUILayout.Space();

        fileName = EditorGUILayout.TextField("File Name", fileName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lane Roots", EditorStyles.boldLabel);
        topLaneRoot = (Transform)EditorGUILayout.ObjectField("Top Lane Root", topLaneRoot, typeof(Transform), true);
        topLaneId = EditorGUILayout.IntField("Top Lane Id", topLaneId);


        if (GUILayout.Button("Preview (Console)"))
        {
            Debug.Log("Preview clicked");
            Preview();
        }
        EditorGUILayout.Space();

        botLaneRoot = (Transform)EditorGUILayout.ObjectField("Bot Lane Root", botLaneRoot, typeof(Transform), true);
        botLaneId = EditorGUILayout.IntField("Bot Lane Id", botLaneId);
        if (GUILayout.Button("Export laneRoutes.json"))
        {
            Debug.Log("Export clicked");
            Export();
        }
    }
    private LaneRouteFile Collect()
    {
        LaneRouteFile file = new LaneRouteFile();

        CollectFromRoot(file, topLaneRoot, topLaneId);
        CollectFromRoot(file, botLaneRoot, botLaneId);

        if (file.lanes.Count == 0)
        {
            Debug.LogWarning("[LaneRouteExporter] 수집된 레인이 없습니다.");
            return null;
        }

        file.lanes.Sort((a, b) => a.laneId.CompareTo(b.laneId));
        return file;
    }

    private void CollectFromRoot(LaneRouteFile file, Transform root, int laneId)
    {
        if (root == null)
        {
            Debug.LogWarning($"[LaneRouteExporter] laneId={laneId} root가 비어있습니다.");
            return;
        }

        List<Transform> children = new List<Transform>();
        foreach (Transform child in root)
            children.Add(child);

        // 이름 뒤 숫자 기준 정렬 (WP_0, WP_1, WP_2 ...)
        children.Sort((a, b) =>
        {
            int numA = ExtractTrailingNumber(a.name);
            int numB = ExtractTrailingNumber(b.name);
            return numA.CompareTo(numB);
        });

        LaneRouteData data = new LaneRouteData();
        data.laneId = laneId;

        foreach (Transform child in children)
        {
            data.waypoints.Add(new Vector3Serializable
            {
                x = child.position.x,
                y = 0f,
                z = child.position.z
            });
        }

        file.lanes.Add(data);
    }

    private int ExtractTrailingNumber(string name)
    {
        int i = name.Length - 1;
        while (i >= 0 && char.IsDigit(name[i])) i--;
        string numStr = name.Substring(i + 1);
        return int.TryParse(numStr, out int n) ? n : 0;
    }


    private void Preview()
    {
        LaneRouteFile file = Collect();
        if (file == null)
        {
            Debug.Log("File is nullptr");
            return;
        }

        foreach (LaneRouteData lane in file.lanes)
        {
            Debug.Log($"[LaneRouteExporter] laneId={lane.laneId}, waypoints={lane.waypoints.Count}");
            for (int i = 0; i < lane.waypoints.Count; i++)
            {
                Vector3Serializable wp = lane.waypoints[i];
                Debug.Log($"  [{i}] ({wp.x:F2}, {wp.y:F2}, {wp.z:F2})");
            }
        }
    }

    private void Export()
    {
        LaneRouteFile file = Collect();
        if (file == null) return;

        string path = EditorUtility.SaveFilePanel(
            "Export Lane Routes (JSON)",
            Application.dataPath + "/NavMeshExport",
            string.IsNullOrEmpty(fileName) ? "laneRoutes" : fileName,
            "json");

        if (string.IsNullOrEmpty(path)) return;

        string json = JsonUtility.ToJson(file, prettyPrint: true);
        File.WriteAllText(path, json);

        Debug.Log($"[LaneRouteExporter] Exported {file.lanes.Count} lanes → {path}");
        AssetDatabase.Refresh();
    }
}
#endif
