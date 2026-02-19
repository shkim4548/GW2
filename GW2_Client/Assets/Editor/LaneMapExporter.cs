// Assets/Editor/LaneMapExporterWindow.cs

using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public class LaneMapExporterWindow : EditorWindow
{
    // ▼ NavGrid 정보 (서버와 동일하게 맞춰야 함)
    public Vector3 origin;
    public int width = 20;
    public int height = 20;
    public float cellSize = 0.5f;

    // ▼ Lane별 Tilemap
    public Tilemap topLaneTilemap;
    public Tilemap midLaneTilemap;
    public Tilemap botLaneTilemap;

    // ▼ Lane Id
    public byte topLaneId = 1;
    public byte midLaneId = 2;
    public byte botLaneId = 3;

    // ▼ 출력 파일 이름
    public string fileName = "laneMap.bin";

    [MenuItem("Tools/Lane Map Exporter")]
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

        if (GUILayout.Button("Export Lane Map"))
        {
            ExportLaneMap();
        }
    }

    private void ExportLaneMap()
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Width/Height가 0 이하입니다.");
            return;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("fileName이 비어 있습니다.");
            return;
        }

        byte[] laneMap = new byte[width * height];

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 center = origin
                    + new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);

                laneMap[z * width + x] = GetLaneIdAt(center);
            }
        }

        string folder = Application.dataPath + "/../NavMeshExport";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = Path.Combine(folder, fileName);
        File.WriteAllBytes(path, laneMap);

        Debug.Log($"Lane map exported: {path}");
        EditorUtility.RevealInFinder(path);
    }

    private byte GetLaneIdAt(Vector3 worldPos)
    {
        // Top
        if (topLaneTilemap != null)
        {
            var cell = topLaneTilemap.WorldToCell(worldPos);
            if (topLaneTilemap.HasTile(cell))
                return topLaneId;
        }

        // Mid
        if (midLaneTilemap != null)
        {
            var cell = midLaneTilemap.WorldToCell(worldPos);
            if (midLaneTilemap.HasTile(cell))
                return midLaneId;
        }

        // Bot
        if (botLaneTilemap != null)
        {
            var cell = botLaneTilemap.WorldToCell(worldPos);
            if (botLaneTilemap.HasTile(cell))
                return botLaneId;
        }

        // 아무 lane도 아니면 0
        return 0;
    }
}
