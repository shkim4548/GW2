using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class CardData
    {

    }

    [Serializable]
    public struct NavGridHeader
    {
        public uint magic;      // 'NRGD'
        public ushort version;  // 1
        public int width;
        public int height;
        public float cellSize;
        public Vector3 origin;
    }

    [Serializable]
    public struct NavGridData
    {
        public NavGridHeader header;
        public GridCell[] cells;
    }

    [Serializable]
    public struct GridCell
    {
        public byte walkable;   // 0 or 1
        public float height;    // NavMesh hit y
        public byte n0;         // +X
        public byte n1;         // -X
        public byte n2;         // +Z
        public byte n3;         // -Z
        public int x;
        public int z;
    }
}