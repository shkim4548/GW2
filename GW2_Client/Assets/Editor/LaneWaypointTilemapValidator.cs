#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LaneWaypointTilemapValidator : EditorWindow
{
    public Tilemap laneTilemap;
    public LaneRouteRoot[] laneRoots; // ¾À¿¡¼­ FindObjectOfType·Î ÀÚµ¿ Ã¤¿öµµ µÊ

    [MenuItem("Tools/Validate Lane Waypoints vs Tilemap")]
    public static void ShowWindow()
    {
        GetWindow<LaneWaypointTilemapValidator>("Lane Waypoint Validator");
    }

    private void OnGUI()
    {
        laneTilemap = (Tilemap)EditorGUILayout.ObjectField("Lane Tilemap", laneTilemap, typeof(Tilemap), true);

        if (GUILayout.Button("Auto-Collect LaneRouteRoots"))
        {
            laneRoots = FindObjectsOfType<LaneRouteRoot>();
        }

        SerializedObject so = new SerializedObject(this);
        SerializedProperty rootsProp = so.FindProperty("laneRoots");
        EditorGUILayout.PropertyField(rootsProp, true);
        so.ApplyModifiedProperties();

        if (GUILayout.Button("Validate"))
        {
            Validate();
        }
    }

    private void Validate()
    {
        if (laneTilemap == null)
        {
            Debug.LogError("Lane Tilemap is null.");
            return;
        }

        if (laneRoots == null || laneRoots.Length == 0)
        {
            Debug.LogWarning("No LaneRouteRoots to validate.");
            return;
        }

        foreach (var root in laneRoots)
        {
            if (root == null) continue;

            var wps = root.GetComponentsInChildren<LaneWaypoint>(false);
            if (wps.Length == 0)
            {
                Debug.LogWarning($"LaneRouteRoot '{root.name}' has no LaneWaypoint.");
                continue;
            }

            foreach (var wp in wps)
            {
                Vector3 worldPos = wp.transform.position;
                Vector3Int cell = laneTilemap.WorldToCell(worldPos);
                var tile = laneTilemap.GetTile(cell);

                if (tile == null)
                {
                    Debug.LogWarning(
                        $"[LaneWaypointTilemapValidator] Waypoint '{wp.name}' (root={root.name}) at world({worldPos.x:F2},{worldPos.z:F2}) is NOT on lane tile (cell={cell}).");
                }
            }
        }

        Debug.Log("[LaneWaypointTilemapValidator] Validation complete.");
    }
}
#endif