using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginScene : BaseScene
{
    [Inject]
    IUIService _uiService;

    protected override void Init()
    {
        base.Init();

        // 지역(Scene Service) 미리 다 등록
        // 일정 시간이 지나면 UI를 띄운다.
        _uiService = Bootstrapper.Instance.UIService;
        StartCoroutine("ShowUICoroutine");
    }

    public override void Clear()
    {
        
    }

    public IEnumerator ShowUICoroutine()
    {
        yield return new WaitForSeconds(5.0f);
        _uiService.ShowSceneUI<UI_LoginScene>();
    }
}
