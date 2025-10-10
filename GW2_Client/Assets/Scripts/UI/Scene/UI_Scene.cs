using UnityEngine;

public class UI_Scene : UI_Base
{
    [Inject]
    protected IUIService uiService;

    public override void Init()
    {
        DI.Container.Inject(this);

        uiService.SetCanvas(gameObject, false);
    }
}
