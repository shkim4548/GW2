using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_LobbyScene : UI_Scene
{
    enum Buttons
    {
        EnterMulti_1vs1,
        EnterMulti_2vs2,
        EnterSingle,
    }

    public override void Init()
    {
        base.Init();
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.EnterMulti_1vs1).gameObject.BindEvent(OnClick1vs1);
        GetButton((int)Buttons.EnterMulti_2vs2).gameObject.BindEvent(OnClick2vs2);
        GetButton((int)Buttons.EnterSingle).gameObject.BindEvent(OnClickSingle);
    }

    public void OnClick1vs1(PointerEventData data)
    {
        ISceneService sceneService = Bootstrapper.Instance.SceneService;
        sceneService.LoadScene(Define.Scene.Game);
    }

    public void OnClick2vs2(PointerEventData data)
    {
        ISceneService sceneService = Bootstrapper.Instance.SceneService;
        sceneService.LoadScene(Define.Scene.Game);
    }

    public void OnClickSingle(PointerEventData data)
    {
        // TODO : TEST만 하기 때문에 그냥 Single플레이만 넘겨버린다.
        ISceneService sceneService = Bootstrapper.Instance.SceneService;
        //sceneService.LoadScene(Define.Scene.Game);
        // 개발단계에서는 여기를 테스트 씬으로 넘겨버리자
        sceneService.LoadScene(Define.Scene.GameScene);
    }
}
