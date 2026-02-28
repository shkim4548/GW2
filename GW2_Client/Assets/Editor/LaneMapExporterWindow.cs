#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LaneMapExporterWindow : EditorWindow
{
    [Header("NavGrid Spec (서버 navgrid.bin과 동일)")]
    public Vector3 origin = Vector3.zero;   // 서버 WalkableGrid.origin 과 동일해야 함
    public int width = 38;
    public int height = 38;
    public float cellSize = 0.5f;

    [Header("Lane Tilemaps")]
    public Tilemap topLaneTilemap;
    public Tilemap midLaneTilemap;
    public Tilemap botLaneTilemap;

    [Header("Lane Ids (서버와 약속된 값)")]
    [Tooltip("0은 off / lane 없음")]
    public byte topLaneId = 1;
    public byte midLaneId = 2;
    public byte botLaneId = 3;

    [Header("Output")]
    public string fileName = "laneMap.bin";

    [MenuItem("Tools/Lane/Lane Map Exporter (bin)")]
    public static void ShowWindow()
    {
        GetWindow<LaneMapExporterWindow>("Lane Map Exporter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("NavGrid Spec (서버와 동일하게 설정)", EditorStyles.boldLabel);
        origin = EditorGUILayout.Vector3Field("Origin", origin);
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Lane Tilemaps", EditorStyles.boldLabel);
        topLaneTilemap = (Tilemap)EditorGUILayout.ObjectField("Top Lane Tilemap", topLaneTilemap, typeof(Tilemap), true);
        midLaneTilemap = (Tilemap)EditorGUILayout.ObjectField("Mid Lane Tilemap", midLaneTilemap, typeof(Tilemap), true);
        botLaneTilemap = (Tilemap)EditorGUILayout.ObjectField("Bot Lane Tilemap", botLaneTilemap, typeof(Tilemap), true);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Lane Ids", EditorStyles.boldLabel);
        topLaneId = (byte)Mathf.Max(0, EditorGUILayout.IntField("Top Lane Id", topLaneId));
        midLaneId = (byte)Mathf.Max(0, EditorGUILayout.IntField("Mid Lane Id", midLaneId));
        botLaneId = (byte)Mathf.Max(0, EditorGUILayout.IntField("Bot Lane Id", botLaneId));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        fileName = EditorGUILayout.TextField("File Name", fileName);

        EditorGUILayout.Space();

        if (GUILayout.Button("Export Lane Map (bin)"))
        {
            ExportLaneMapBin();
        }
    }

    private void ExportLaneMapBin()
    {
        if (width <= 0 || height <= 0 || cellSize <= 0f)
        {
            Debug.LogError("[LaneMapExporter] Invalid NavGrid spec.");
            return;
        }

        // 저장 경로 선택
        string path = EditorUtility.SaveFilePanel(
            "Export Lane Map (bin)",
            Application.dataPath,
            string.IsNullOrEmpty(fileName) ? "laneMap" : fileName,
            "bin");

        if (string.IsNullOrEmpty(path))
            return;

        int cellCount = width * height;
        byte[] laneBuf = new byte[cellCount];

        // 기본값은 lane 없음 (0)
        for (int i = 0; i < laneBuf.Length; ++i)
            laneBuf[i] = 0;

        // helper 로컬 함수
        void PaintLane(Tilemap tilemap, byte laneId)
        {
            if (tilemap == null)
                return;
            if (laneId == 0)
                return;

            for (int z = 0; z < height; ++z)
            {
                for (int x = 0; x < width; ++x)
                {
                    // 셀 중심 월드 좌표 (NavGrid와 동일한 방식)
                    float wx = origin.x + (x + 0.5f) * cellSize;
                    float wz = origin.z + (z + 0.5f) * cellSize;

                    Vector3 worldPos = new Vector3(wx, 0f, wz);
                    Vector3Int cell = tilemap.WorldToCell(worldPos);
                    var tile = tilemap.GetTile(cell);

                    if (tile != null)
                    {
                        int idx = z * width + x;
                        laneBuf[idx] = laneId; // 중복 시 마지막 적용 lane이 덮어씀 (우선순위 정책은 필요시 조정)
                    }
                }
            }
        }

        // 순서대로 lane 칠하기 (top→mid→bot)
        PaintLane(topLaneTilemap, topLaneId);
        PaintLane(midLaneTilemap, midLaneId);
        PaintLane(botLaneTilemap, botLaneId);

        // bin 쓰기
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            // header
            // magic 'LNMP'
            bw.Write(0x4C4E4D50); // 'L' 'N' 'M' 'P'
            bw.Write((ushort)1);  // version = 1
            bw.Write((ushort)0);  // reserved / alignment

            bw.Write(width);
            bw.Write(height);

            // (선택) origin, cellSize도 넣고 싶으면 여기 추가 가능 - 서버 측도 맞춰서 읽어야 함 (현재는 생략)

            // data: width * height 개의 laneId (row-major, z * width + x)
            bw.Write(laneBuf);
        }

        Debug.Log($"[LaneMapExporter] Lane map exported to: {path}");
    }
}
#endif