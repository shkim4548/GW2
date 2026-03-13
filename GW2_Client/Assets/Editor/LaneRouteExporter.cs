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

        if (GUILayout.Button("Preview (Console)"))
        {
            Preview();
        }

        if (GUILayout.Button("Export laneRoutes.json"))
        {
            Export();
        }
    }

    private LaneRouteFile Collect()
    {
        LaneRouteRoot[] roots = FindObjectsOfType<LaneRouteRoot>();

        if (roots.Length == 0)
        {
            Debug.LogWarning("[LaneRouteExporter] LaneRouteRoot 컴포넌트가 씬에 없습니다.");
            return null;
        }

        LaneRouteFile file = new LaneRouteFile();

        foreach (LaneRouteRoot root in roots)
        {
            LaneWaypoint[] wps = root.GetComponentsInChildren<LaneWaypoint>(false);

            if (wps.Length == 0)
            {
                Debug.LogWarning($"[LaneRouteExporter] '{root.name}' (laneId={root.laneId}) 에 LaneWaypoint가 없습니다. 건너뜁니다.");
                continue;
            }

            // order 기준 정렬
            List<LaneWaypoint> sorted = new List<LaneWaypoint>(wps);
            sorted.Sort((a, b) => a.order.CompareTo(b.order));

            LaneRouteData data = new LaneRouteData();
            data.laneId = root.laneId;

            foreach (LaneWaypoint wp in sorted)
            {
                data.waypoints.Add(Vector3Serializable.FromVector3(wp.transform.position, root.exportY));
            }

            file.lanes.Add(data);
        }

        // laneId 오름차순 정렬
        file.lanes.Sort((a, b) => a.laneId.CompareTo(b.laneId));

        return file;
    }

    private void Preview()
    {
        LaneRouteFile file = Collect();
        if (file == null) return;

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
