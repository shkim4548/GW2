using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyScene : BaseScene
{
    public IUIService _uiService;
    public INetworkService _networkService;

    protected override void Init()
    {
        base.Init();
        _uiService = Bootstrapper.Instance.UIService;
        _networkService = Bootstrapper.Instance.NetworkService;
        _uiService.ShowSceneUI<UI_LobbyScene>();

        C_ENTER_LOBBY enterLobbyRequest = new C_ENTER_LOBBY();
        //var networkService = DI.Container.Resolve<INetworkService>();
        //networkService.SetNetworkId(recvLoginpkt.PlayerIndex);
        //_networkService.SetNetworkId(1);
        _networkService.Send(enterLobbyRequest);
    }

    public override void Clear()
    {

    }
}
