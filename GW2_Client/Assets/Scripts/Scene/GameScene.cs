using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScene : BaseScene
{
    [Inject]
    IResourceService _resourceService;

    protected override void Init()
    {
        base.Init();
        //INetworkService network = DI.Container.Resolve<INetworkService>();
        INetworkService network = Bootstrapper.Instance.NetworkService;

        //_resourceService.Instantiate("Player/Police");
        // TODO : 하드코딩된 RoomId 변경
        C_ENTER_GAME enterGamePkt = new C_ENTER_GAME();
        enterGamePkt.PlayerIndex = network.GetNetworkId();
        enterGamePkt.RoomId = 0;
        network.Send(enterGamePkt);
    }

    public override void Clear()
    {

    }
}
