using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScene : BaseScene
{
    protected override void Init()
    {
        base.Init();
        INetworkService network = Bootstrapper.Instance.NetworkService;
        //_resourceService.Instantiate("Player/Police");
        // TODO : 하드코딩된 RoomId 변경

        C_ENTER_GAME enterGamePkt = new C_ENTER_GAME();
        //enterGamePkt.PlayerIndex = network.GetNetworkId();
        enterGamePkt.PlayerIndex = 1;
        //Debug.Log(enterGamePkt.PlayerIndex);
        enterGamePkt.RoomId = 0;
        network.Send(enterGamePkt);
        Debug.Log("TestScene");

        LaneDebugProbe probe = GetComponent<LaneDebugProbe>();
        probe.CheckLaneTile();
    }

    public override void Clear()
    {
        
    }
}
