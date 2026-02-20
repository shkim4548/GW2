using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneRouteRoot : MonoBehaviour
{
    [Tooltip("LaneId using in server")]
    public int laneId = 1;

    [Tooltip("Y좌표 내보낼지 여부 ")]
    public bool exportY = false;
}
