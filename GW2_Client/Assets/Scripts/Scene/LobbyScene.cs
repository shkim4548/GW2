using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyScene : BaseScene
{
    public IUIService _uiService;

    protected override void Init()
    {
        base.Init();
        _uiService = DI.Container.Resolve<IUIService>();
        _uiService.ShowSceneUI<UI_LobbyScene>();

        C_ENTER_LOBBY enterLobbyRequest = new C_ENTER_LOBBY();
        var networkService = DI.Container.Resolve<INetworkService>();
        //networkService.SetNetworkId(recvLoginpkt.PlayerIndex);
        networkService.SetNetworkId(1);
        networkService.Send(enterLobbyRequest);
    }

    public override void Clear()
    {

    }
}
