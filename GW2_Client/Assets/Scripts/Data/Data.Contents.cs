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

    /*-------------------------
        Navmesh Server Data
     --------------------------*/

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

    /*--------------------------------
        Minion Line data for server
     ---------------------------------*/
    [Serializable]
    public class LaneRouteFile
    {
        public List<LaneRouteData> lanes = new();
    }

    [Serializable]
    public class LaneRouteData
    {
        public int laneId;
        public List<Vector3Serializable> waypoints = new();
    }

    [Serializable]
    public struct Vector3Serializable
    {
        public float x, y, z;

        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }

        public static Vector3Serializable FromVector3(Vector3 v, bool exportY)
        {
            return new Vector3Serializable(v.x, exportY ? v.y : 0f, v.z);
        }
    }

    [Serializable]
    public struct CardInfo
    {
        public int id;
        public string name;      // 추가 — 표시용 이름
        public string skill_id;  // 스킬 연결 ID
        public int damage;
        public int price;
    }

    [Serializable]
    public class CardInfoList : ILoader<int, CardInfo>   // struct → class로 변경
    {
        public List<CardInfo> cards;

        public Dictionary<int, CardInfo> MakeDict()
        {
            Dictionary<int, CardInfo> dict = new Dictionary<int, CardInfo>();
            foreach (CardInfo card in cards)
                dict.Add(card.id, card);
            return dict;
        }
    }
}