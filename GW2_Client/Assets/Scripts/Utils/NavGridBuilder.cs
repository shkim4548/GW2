using Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class NavGridBuilder
{
    public static GridCell[] BuildNavGrid(
        Bounds worldBounds,
        float cellSize,
        out int width,
        out int height,
        out Vector3 origin
    )
    {
        origin = worldBounds.min;

        width = Mathf.CeilToInt(worldBounds.size.x / cellSize);
        height = Mathf.CeilToInt(worldBounds.size.z / cellSize);

        GridCell[] cells = new GridCell[width * height];

        int index = 0;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 samplePos = new Vector3(
                    origin.x + x * cellSize + cellSize * 0.5f,
                    worldBounds.max.y + 1.0f,
                    origin.z + z * cellSize + cellSize * 0.5f
                );

                GridCell cell = new GridCell();
                cell.x = x;
                cell.z = z;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(samplePos, out hit, cellSize * 0.5f, NavMesh.AllAreas))
                {
                    cell.walkable = 1;
                    cell.height = hit.position.y;
                }
                else
                {
                    cell.walkable = 0;
                    cell.height = 0f;
                }

                cells[index++] = cell;
            }
        }

        // 4방향 neighbor 계산
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = z * width + x;
                if (cells[i].walkable == 0)
                    continue;

                cells[i].n0 = (x + 1 < width && cells[z * width + (x + 1)].walkable == 1) ? (byte)1 : (byte)0;
                cells[i].n1 = (x - 1 >= 0 && cells[z * width + (x - 1)].walkable == 1) ? (byte)1 : (byte)0;
                cells[i].n2 = (z + 1 < height && cells[(z + 1) * width + x].walkable == 1) ? (byte)1 : (byte)0;
                cells[i].n3 = (z - 1 >= 0 && cells[(z - 1) * width + x].walkable == 1) ? (byte)1 : (byte)0;
            }
        }

        return cells;
    }
}
