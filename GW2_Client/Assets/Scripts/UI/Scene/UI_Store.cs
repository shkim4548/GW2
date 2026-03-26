using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Google.Protobuf.Protocol;
using UnityEngine.EventSystems;

public class UI_Store : UI_Popup
{
    public static System.Action<long> OnGoldUpdate;
    public static System.Action<bool, int, long> OnBuyResult;
    public static System.Action<List<int>> OnDeckSync;

    private int _selectedCardId = -1;
    private long _currentGold = 0;
    private List<int> _deckCardIds = new List<int>();

    enum Buttons
    {
        StoreTag,
        DeleteTag,
        Buy,
        Close,
    }

    enum Texts
    {
        PriceText,
        CoinPriceText,
    }

    public override void Init()
    {
        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Buy).gameObject.BindEvent(HandleBuyButton);
        GetButton((int)Buttons.DeleteTag).gameObject.BindEvent(HandleDeleteTagButton);
        GetButton((int)Buttons.StoreTag).gameObject.BindEvent(HandleStoreTagButton);
    }

    public void HandleBuyButton(PointerEventData data)
    {

    }

    public void HandleStoreTagButton(PointerEventData data)
    {

    }

    public void HandleDeleteTagButton(PointerEventData data)
    {

    }
}
