using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using Google.Protobuf.Protocol;

public class UI_Store : UI_Scene
{
    public static event System.Action<long> OnGoldChanged;
    enum GameObjects
    {
        store,
        strengthen,
        delete,
        StoreContents,
        DeleteContent,
    }

    enum Texts
    {
        Coin_txt,
        Buy_txt,
        Card_Price,
    }

    enum Buttons
    {
        Buy
    }
    private int _selectedCardId = -1;
    private long _currentGold = 0;

    private static System.Action<long> OnGoldUpdate;
    private static System.Action<bool, int, long> OnBuyResult;

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
        Bind<TMP_Text>(typeof(Texts));
        Bind<UnityEngine.UI.Button>(typeof(Buttons));

        GetButton((int)Buttons.Buy).onClick.AddListener(OnClickBuy);

        OnGoldUpdate -= HandleGoldUpdate;
        OnGoldUpdate += HandleGoldUpdate;
        OnBuyResult -= HandleBuyResult;
        OnBuyResult += HandleBuyResult;

        // 기본 탭은 store 탭이다
        ShowTab(GetObject((int)GameObjects.store));
    }

    public void OnClickStoreTab(PointerEventData data)
    {
        ShowTab(GetObject((int)GameObjects.store));
    }

    public void OnClickStrengthenTab(PointerEventData data)
    {
        ShowTab(GetObject((int)GameObjects.strengthen));
    }

    public void OnClickDeleteTab(PointerEventData data)
    {
        ShowTab(GetObject((int)GameObjects.delete));
    }

    // 탭 전환(삭제 - 구매)
    private void ShowTab(GameObject target)
    {
        GetObject((int)GameObjects.store).SetActive(false);
        GetObject((int)GameObjects.strengthen).SetActive(false);
        GetObject((int)GameObjects.delete).SetActive(false);
        target.SetActive(true);
    }

    // 카드 목록 갱신
    public void PopulateStore(List<Data.CardInfo> cards)
    {
        Transform content = GetObject((int)GameObjects.StoreContents).transform;
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach(var card in cards)
        {
            // TODO: 카드 슬롯 프리팹 인스턴스화
            // GameObject slot = Instantiate(cardSlotPrefab, content);
            // slot.GetComponent<UI_CardSlot>().Set(card, OnSelectCard);
        }
    }

    public void PopulateDelete(List<int> deckCardIds)
    {
        Transform content = GetObject((int)GameObjects.DeleteContent).transform;
        foreach (Transform child in content)
        {
            // TODO: 삭제용 카드 슬롯 인스턴스화
        }
    }

    // 카드 선택
    public void OnSelectedCard(int cardId, long price)
    {
        _selectedCardId = cardId;
        GetText((int)Texts.Card_Price).text = $"{price} G";
    }

    // 구매 버튼 클릭시
    public void OnClickBuy()
    {
        if (_selectedCardId < 0)
            return;

        C_BUY_CARD pkt = new C_BUY_CARD();
        pkt.RoomId = Bootstrapper.Instance.NetworkService.GetRoomId();
        pkt.CardId = _selectedCardId;
        Bootstrapper.Instance.NetworkService.Send(pkt);
    }

    private void HandleGoldUpdate(long gold)
    {
        _currentGold = gold;
        GetText((int)Texts.Coin_txt).text = gold.ToString();
    }

    public void HandleBuyResult(bool success, int cardId, long gold)
    {
        HandleGoldUpdate(gold);
        if (success)
            Debug.Log($"[UI_Store] 카드 {cardId} 구매 성공");
        else
            Debug.Log($"[UI_Store] 골드 부족");
    }

    private void OnDestroy()
    {
        OnGoldUpdate -= HandleGoldUpdate;
        OnBuyResult -= HandleBuyResult;
    }
}