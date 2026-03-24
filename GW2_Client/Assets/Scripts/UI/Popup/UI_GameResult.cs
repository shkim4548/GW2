using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Google.Protobuf.Enum;

public class UI_GameResult : UI_Popup
{
    public static Action<CampType> OnGameEnd;

    enum Texts { Result_txt }
    enum Buttons { BackToLobby_btn }

    public override void Init()
    {
        Bind<Text>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.BackToLobby_btn).gameObject.BindEvent(OnClickBackToLobby);

        OnGameEnd -= HandleGameEnd;
        OnGameEnd += HandleGameEnd;
    }

    private void HandleGameEnd(CampType winner)
    {
        var myPlayer = Bootstrapper.Instance.ObjectService.MyPlayer;
        bool isWin = myPlayer != null && myPlayer.CampType == winner;
        GetText((int)Texts.Result_txt).text = isWin ? "VICTORY" : "DEFEAT";
    }

    private void OnClickBackToLobby(PointerEventData data)
    {
        OnGameEnd = null;
        Bootstrapper.Instance.SceneService.LoadScene(Define.Scene.Lobby);
    }

    private void OnDestroy()
    {
        OnGameEnd -= HandleGameEnd;
    }
}
