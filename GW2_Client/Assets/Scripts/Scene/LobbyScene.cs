using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyScene : BaseScene
{
    //[LazyInject]
    //public Lazy<IUIService> _uiService;

    protected override void Init()
    {
        base.Init();
        _uiService.Value.ShowSceneUI<UI_LobbyScene>();
    }

    public override void Clear()
    {

    }
}
