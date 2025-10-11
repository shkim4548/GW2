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
        INetworkService network = DI.Container.Resolve<INetworkService>();

        //_resourceService.Instantiate("Player/Police");
        C_ENTER_GAME enterGamePkt = new C_ENTER_GAME();
        network.Send(enterGamePkt);
    }

    public override void Clear()
    {

    }
}
