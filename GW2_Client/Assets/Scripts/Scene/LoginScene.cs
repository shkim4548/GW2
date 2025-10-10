using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginScene : BaseScene
{
    [Inject]
    protected INetworkService _networkService;

    protected override void Init()
    {
        base.Init();
        _networkService.Init();
        // 일정 시간이 지나면 UI를 띄운다.
        StartCoroutine("ShowUICoroutine");
    }

    public override void Clear()
    {
        
    }

    public IEnumerator ShowUICoroutine()
    {
        yield return new WaitForSeconds(5.0f);
        _uiService.Value.ShowSceneUI<UI_LoginScene>();
    }
}
