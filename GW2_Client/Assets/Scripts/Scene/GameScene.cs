using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScene : BaseScene
{
    [Inject]
    IResourceService _resourceService;

    private UI_StatusBar _statusBar;

    protected override void Init()
    {
        base.Init();
        //INetworkService network = DI.Container.Resolve<INetworkService>();
        INetworkService network = Bootstrapper.Instance.NetworkService;

        IUIService uiService = Bootstrapper.Instance.UIService;
        //uiService.ShowSceneUI<UI_GameScene>();
        uiService.ShowPopupUI<UI_Select>();
    }

    public override void Clear()
    {

    }

    public void UpdateHpBar(float current, float max)
    {
        if (_statusBar != null)
            _statusBar.SetHp(current, max);
    }
}
