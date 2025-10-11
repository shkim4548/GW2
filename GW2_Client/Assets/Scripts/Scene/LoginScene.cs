using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginScene : BaseScene
{
    [Inject]
    IUIService uiService;

    protected override void Init()
    {
        base.Init();

        // 지역(Scene Service) 미리 다 등록
        // 일정 시간이 지나면 UI를 띄운다.
        uiService = DI.Container.Resolve<IUIService>();
        StartCoroutine("ShowUICoroutine");
    }

    public override void Clear()
    {
        
    }

    public IEnumerator ShowUICoroutine()
    {
        yield return new WaitForSeconds(5.0f);
        uiService.ShowSceneUI<UI_LoginScene>();
    }
}
