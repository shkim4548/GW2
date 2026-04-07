using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Google.Protobuf.Enum;
using Google.Protobuf.Protocol;

public class UI_Select : UI_Popup
{
    public static System.Action<int, PlayerType, bool> OnCharSelected;

    enum Buttons
    {
        Character01,
        Character02,
        Character03,
        Character04,
        Confirm,
    }

    private PlayerType _selectedType = PlayerType.None;
    private int _roomId;

    public override void Init()
    {
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Character01).onClick.AddListener(() => OnClickChar(PlayerType.Police));
        GetButton((int)Buttons.Character02).onClick.AddListener(() => OnClickChar(PlayerType.Monk));
        GetButton((int)Buttons.Character03).onClick.AddListener(() => OnClickChar(PlayerType.Lightsabre));
        GetButton((int)Buttons.Character04).onClick.AddListener(() => OnClickChar(PlayerType.Firefighter));

        GetButton((int)Buttons.Confirm).onClick.AddListener(OnClickConfirm);
        GetButton((int)Buttons.Confirm).interactable = false;

        OnCharSelected -= HandleCharSelected;
        OnCharSelected += HandleCharSelected;

        //_roomId = PlayerPrefs.GetInt("CurrentRoomId", 0);
        //_roomId = Bootstrapper.Instance.NetworkService.GetRoomId();
        _roomId = 0;
    }

    private void OnClickChar(PlayerType type)
    {
        _selectedType = type;
        GetButton((int)Buttons.Confirm).interactable = true;

        C_SELECT_CHARACTER pkt = new C_SELECT_CHARACTER();
        pkt.RoomId = _roomId;
        pkt.PlayerType = type;
        Bootstrapper.Instance.NetworkService.Send(pkt);
    }

    private void HandleCharSelected(int playerId, PlayerType type, bool isCancel)
    {
        int myId = Bootstrapper.Instance.NetworkService.GetNetworkId();
        if (playerId == myId) return;

        Button btn = GetButtonByType(type);
        if (btn != null)
            btn.interactable = isCancel;
    }

    private void OnClickConfirm()
    {
        Debug.Log($"{_selectedType}, Confirm Clicked");

        if (_selectedType == PlayerType.None) 
            return;

        C_CONFIRM_CHARACTER pkt = new C_CONFIRM_CHARACTER();
        pkt.RoomId = _roomId;
        pkt.PlayerType = _selectedType;
        Bootstrapper.Instance.NetworkService.Send(pkt);

        IUIService ui = Bootstrapper.Instance.UIService;
        ui.ClosePopupUI();
    }

    private Button GetButtonByType(PlayerType type)
    {
        return type switch
        {
            PlayerType.Police => GetButton((int)Buttons.Character01),
            PlayerType.Monk => GetButton((int)Buttons.Character02),
            PlayerType.Lightsabre => GetButton((int)Buttons.Character03),
            PlayerType.Firefighter => GetButton((int)Buttons.Character04),
            _ => null
        };
    }

    private void OnDestroy()
    {
        OnCharSelected -= HandleCharSelected;
    }
}
