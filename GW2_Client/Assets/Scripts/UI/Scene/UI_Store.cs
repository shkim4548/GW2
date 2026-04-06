using Google.Protobuf.Protocol;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

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

    enum GameObjects
    {
        StoreTab,     // 상점 탭 패널 전체
        DeleteTab,    // 덱 탭 패널 전체
        StoreContent,   // 상점 ScrollView → Viewport → Content
        DeleteContent,  // 덱   ScrollView → Viewport → Content
    }

    public override void Init()
    {
        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));

        GetButton((int)Buttons.Buy).gameObject.BindEvent(HandleBuyButton);
        GetButton((int)Buttons.DeleteTag).gameObject.BindEvent(HandleDeleteTagButton);
        GetButton((int)Buttons.StoreTag).gameObject.BindEvent(HandleStoreTagButton);

        UI_CardSlot.OnCardSelected -= OnCardSelected;
        UI_CardSlot.OnCardSelected += OnCardSelected;
        OnGoldUpdate -= HandleGoldUpdate;
        OnGoldUpdate += HandleGoldUpdate;
        OnBuyResult -= HandleBuyResult;
        OnBuyResult += HandleBuyResult;
        OnDeckSync -= HandleDeckSync;
        OnDeckSync += HandleDeckSync;

        PopulateStore();
        PopulateDelete();
        ShowTab(true);
    }

    // 카드 리스트 채우기
    private void PopulateStore()
    {
        Transform content = GetObject((int)GameObjects.StoreContent).transform;
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach(Data.CardInfo card in Bootstrapper.Instance.DataService.CardDict.Values)
        {
            GameObject slot = Bootstrapper.Instance.ResourceService.Instantiate("UI/SubItem/UI_CardSlot", content);
            slot.GetComponent<UI_CardSlot>().SetStoreMode(card);
        }
    }

    // 소지한 카드
    private void PopulateDelete()
    {
        Transform content = GetObject((int)GameObjects.DeleteContent).transform;
        foreach(Transform child in content)
            Destroy(child.gameObject);

        foreach(int cardId in _deckCardIds)
        {
            if (!Bootstrapper.Instance.DataService.CardDict.TryGetValue(cardId, out var card))
                continue;
            GameObject slot = Bootstrapper.Instance.ResourceService
                .Instantiate("UI/SubItem/UI_CardSlot", content);
            slot.GetComponent<UI_CardSlot>().SetStoreMode(card);
        }
    }

    private void ShowTab(bool isStore)
    {
        GetObject((int)GameObjects.StoreTab).SetActive(isStore);
        GetObject((int)GameObjects.DeleteTab).SetActive(!isStore);
    }

    private void OnCardSelected(int cardId, int price)
    {
        _selectedCardId = cardId;
        GetText((int)Texts.PriceText).text = $"{price} G";
        GetButton((int)Buttons.Buy).interactable = true;
    }

    private void HandleDeckSync(List<int> deckCardIds)
    {
        _deckCardIds = deckCardIds;
        PopulateDelete();
    }

    private void HandleBuyResult(bool success, int cardId, long gold)
    {
        HandleGoldUpdate(gold);
        if (success)
        {
            _deckCardIds.Add(cardId);
            PopulateDelete();
        }
    }

    public void HandleBuyButton(PointerEventData data)
    {
        if (_selectedCardId < 0)
            return;

        C_BUY_CARD pkt = new C_BUY_CARD();
        pkt.CardId = _selectedCardId;
        Bootstrapper.Instance.NetworkService.Send(pkt);
    }

    public void HandleStoreTagButton(PointerEventData data)
    {
        ShowTab(true);
    }

    public void HandleDeleteTagButton(PointerEventData data)
    {
        ShowTab(false);
    }

    public void HandleGoldUpdate(long gold)
    {
        _currentGold = gold;
        GetTMP((int)Texts.CoinPriceText).text = $"{gold} G";
    }

    private void OnDestroy()
    {
        UI_CardSlot.OnCardSelected -= OnCardSelected;
        OnGoldUpdate -= HandleGoldUpdate;
        OnBuyResult -= HandleBuyResult;
        OnDeckSync -= HandleDeckSync;
    }
}
