using Data;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LaneRouteExporter
{
    [MenuItem("Tools/Export Lane Routes To JSON")]
    public static void ExportLaneRoutes()
    {
        Debug.Log("Export Lane Routes now start");
        // 현재 씬에서 모든 LaneRouteRoot 찾기
        var roots = GameObject.FindObjectsOfType<LaneRouteRoot>();

        if (roots.Length == 0)
        {
            Debug.LogWarning("LaneRouteRoot 컴포넌트를 가진 오브젝트가 없습니다.");
            return;
        }

        var file = new LaneRouteFile();

        foreach (var root in roots)
        {
            if (root.laneId <= 0)
            {
                Debug.LogWarning($"LaneRouteRoot '{root.name}' 의 laneId가 0 이하입니다. 스킵합니다.");
                continue;
            }

            var laneData = new LaneRouteData
            {
                laneId = root.laneId
            };

            // 1) 자식들 중 LaneWaypoint가 붙어있으면 그 기준으로 정렬
            var waypoints = root.GetComponentsInChildren<LaneWaypoint>(includeInactive: false)
                                .OrderBy(wp => wp.order)
                                .Select(wp => wp.transform)
                                .ToList();

            // 2) 없다면, 직접 자식 Transform 순서 사용
            if (waypoints.Count == 0)
            {
                waypoints = new System.Collections.Generic.List<Transform>();
                foreach (Transform child in root.transform)
                    waypoints.Add(child);
            }

            if (waypoints.Count == 0)
            {
                Debug.LogWarning($"LaneRouteRoot '{root.name}' (laneId={root.laneId}) 에 웨이포인트가 없습니다.");
                continue;
            }

            foreach (var t in waypoints)
            {
                var pos = t.position; // 월드 좌표
                laneData.waypoints.Add(Vector3Serializable.FromVector3(pos, root.exportY));
            }

            file.lanes.Add(laneData);
        }

        if (file.lanes.Count == 0)
        {
            Debug.LogWarning("내보낼 LaneRoute가 없습니다.");
            return;
        }

        // 저장 경로 선택
        string path = EditorUtility.SaveFilePanel(
            "Export Lane Routes",
            Application.dataPath,
            "laneRoutes",
            "json");

        if (string.IsNullOrEmpty(path))
            return;

        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(path, json);

        Debug.Log($"Lane routes exported to: {path}");
    }
}