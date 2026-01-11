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
    public struct GridCell
    {
        public byte flags;   // bit 0: walkable
        public byte links;   // 4πÊ«‚ neighbors bitmask
        public float height;
    }


    [Serializable]
    public struct NavGridData
    {
        public NavGridHeader header;
        public GridCell[] cells;
    }

}