using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using Google.Protobuf.Protocol;

// 주의사항 : UI의 버튼 이벤트는 유니티의 특성상 반드시 DIInstaller보다 빠르게 호출된다. 명시적 주입할 것
public class UI_LoginScene : UI_Scene
{
    DIContainer _container;

    enum UIButtons
    {
        LoginButton,
    }

    enum UIInputFields
    {
        NicknameInput,
    }

    public override void Init()
    {
        Bind<Button>(typeof(UIButtons));
        Bind<TMP_InputField>(typeof(UIInputFields));

        GetButton((int)UIButtons.LoginButton).gameObject.BindEvent(OnLoginButtonClick);
        //DI.Container.Resolve<INetworkService>();
    }

    public void OnLoginButtonClick(PointerEventData evt)
    {
        string text = Get<TMP_InputField>((int)UIInputFields.NicknameInput).GetComponent<TMP_InputField>().text;

        Debug.Log(text);

        C_LOGIN loginPkt = new C_LOGIN() { Nickname = text };
        var networkService = DI.Container.Resolve<INetworkService>();

        networkService.Send(loginPkt);
    }

    public void OnExitButtonClick(PointerEventData evt)
    {
        Debug.Log("Exit Button Clicked");
        Application.Quit();
    }
}
