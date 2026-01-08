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
    public struct GridCell
    {
        public bool walkable;
        public float height;
    }

    [Serializable]
    public class NavGridData
    {
        public int width;
        public int height;
        public float cellSize;
        public Vector3 origin;
        public GridCell[] cells;
    }

}