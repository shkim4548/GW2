#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 
/// </summary>
public class LaneNavGridAlignmentChecker : EditorWindow
{
    public Tilemap laneTilemap;
    public Vector3 navGridOrigin; // 서버 navgrid.bin에서 사용한 origin
    public int navGridWidth;
    public int navGridHeight;
    public float cellSize;

    [MenuItem("Tools/Check Lane Tilemap Alignment")]
    public static void ShowWindow()
    {
        GetWindow<LaneNavGridAlignmentChecker>("Lane Alignment Checker");
    }

    private void OnGUI()
    {
        laneTilemap = (Tilemap)EditorGUILayout.ObjectField("Lane Tilemap", laneTilemap, typeof(Tilemap), true);
        navGridOrigin = EditorGUILayout.Vector3Field("NavGrid Origin", navGridOrigin);
        navGridWidth = EditorGUILayout.IntField("NavGrid Width", navGridWidth);
        navGridHeight = EditorGUILayout.IntField("NavGrid Height", navGridHeight);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);

        if (GUILayout.Button("Check Alignment"))
        {
            CheckAlignment();
        }
    }

    private void CheckAlignment()
    {
        if (laneTilemap == null)
        {
            Debug.LogError("Lane Tilemap is null.");
            return;
        }

        int outOfBoundsCount = 0;

        // Tilemap 전체 bounds 순회
        var bounds = laneTilemap.cellBounds;
        for (int y = bounds.yMin; y < bounds.yMax; ++y)
        {
            for (int x = bounds.xMin; x < bounds.xMax; ++x)
            {
                var pos = new Vector3Int(x, y, 0);
                var tile = laneTilemap.GetTile(pos);
                if (tile == null)
                    continue;

                // 1) 타일의 월드 중심 좌표
                Vector3 worldPos = laneTilemap.GetCellCenterWorld(pos);

                // 2) 서버와 동일한 WorldToGrid 계산
                float localX = (worldPos.x - navGridOrigin.x) / cellSize;
                float localZ = (worldPos.z - navGridOrigin.z) / cellSize;

                // 서버 구현과 최대한 동일하게
                int gx = Mathf.FloorToInt(localX + 0.5f);
                int gz = Mathf.FloorToInt(localZ + 0.5f);

                if (gx < 0 || gz < 0 || gx >= navGridWidth || gz >= navGridHeight)
                {
                    outOfBoundsCount++;
                    Debug.LogWarning($"Tile at {pos} -> world({worldPos.x:F2},{worldPos.z:F2}) -> grid({gx},{gz}) OUT OF BOUNDS");
                }
            }
        }

        if (outOfBoundsCount == 0)
            Debug.Log("[LaneNavGridAlignmentChecker] All lane tiles mapped inside NavGrid bounds.");
        else
            Debug.LogWarning($"[LaneNavGridAlignmentChecker] Out of bounds tiles: {outOfBoundsCount}");
    }
}
#endif