using Google.Protobuf.Enum;
using System.Collections.Generic;
using UnityEngine;

public class BaronController : MinionController
{
    private const float Y_OFFSET = 1.5f;

    public override void Init()
    {
        base.Init();
        CampType = CampType.CampNeutural;

        // 스폰 위치 Y 보정
        // ObjectService가 transform.position을 설정한 뒤 Init()을 호출하므로
        // 여기서 Y를 올려줌
        var pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y + Y_OFFSET, pos.z);
    }

    // 이동 경로 Y 보정
    // PacketHandler에서 MinionController로 캐스팅하므로 반드시 override 사용
    public override void SetNavPath(List<Vector3> path)
    {
        if (path != null)
        {
            for (int i = 0; i < path.Count; i++)
                path[i] = new Vector3(path[i].x, path[i].y + Y_OFFSET, path[i].z);
        }
        base.SetNavPath(path);
    }

    // 서버 보정 위치 Y 보정
    // ApplyServerCorrection의 스냅이 Y=0으로 내려가는 것을 방지
    public override void SetServerRefPos(Vector3 serverPos, int serverTime)
    {
        base.SetServerRefPos(
            new Vector3(serverPos.x, serverPos.y + Y_OFFSET, serverPos.z),
            serverTime);
    }
}