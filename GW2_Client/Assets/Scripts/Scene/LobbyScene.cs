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
    }

    public override void Clear()
    {

    }
}
