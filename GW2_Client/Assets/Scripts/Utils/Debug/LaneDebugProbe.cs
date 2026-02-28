using UnityEngine;
using UnityEngine.Tilemaps;

public class LaneDebugProbe : MonoBehaviour
{
    public Tilemap laneTilemap;
    public Vector3 probeWorldPos; // ¿¹: (6.61, 0, 5.79)

    [ContextMenu("Check Lane Tile At Probe")]
    public void CheckLaneTile()
    {
        if (laneTilemap == null)
        {
            Debug.LogError("laneTilemap is null.");
            return;
        }

        Vector3Int cell = laneTilemap.WorldToCell(probeWorldPos);
        var tile = laneTilemap.GetTile(cell);

        Debug.Log($"Probe worldPos={probeWorldPos} -> cell={cell}, tile={tile}");
    }
}