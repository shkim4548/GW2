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
        _resourceService.Instantiate("Player/Police");
    }

    public override void Clear()
    {

    }
}
