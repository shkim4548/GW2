using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UI_CardSlot : UI_Base
{
    public static Action<int, int> OnCardSelected;

    enum Images { CardIcon }                                    // 카드 아트워크
    enum TMPs { Key_txt, CardName_txt, Damage_txt, Price_txt } // TMP로 변경
    enum GameObjects { StoreInfo, HandInfo }

    private int _cardId;
    private int _price;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(TMPs));          // Text → TMP_Text
        Bind<GameObject>(typeof(GameObjects));

        GetComponent<Button>()?.onClick.AddListener(OnClick);
    }

    public void SetHandMode(int cardId, string keyLabel)
    {
        _cardId = cardId;
        GetObject((int)GameObjects.StoreInfo).SetActive(false);
        GetObject((int)GameObjects.HandInfo).SetActive(true);
        GetTMP((int)TMPs.Key_txt).text = keyLabel;
        LoadIcon(cardId);
    }

    public void SetStoreMode(Data.CardInfo card)
    {
        _cardId = card.id;
        _price = card.price;
        GetObject((int)GameObjects.StoreInfo).SetActive(true);
        GetObject((int)GameObjects.HandInfo).SetActive(false);
        GetTMP((int)TMPs.CardName_txt).text = card.name;
        GetTMP((int)TMPs.Damage_txt).text = $"DMG {card.damage}";
        GetTMP((int)TMPs.Price_txt).text = $"{card.price} G";
        LoadIcon(card.id);
    }

    private void LoadIcon(int cardId)
    {
        Sprite sprite = Bootstrapper.Instance.DataService.CardIcons?.Get(cardId);
        Image icon = GetImage((int)Images.CardIcon);

        Debug.Log($"[LoadIcon] cardId={cardId} sprite={sprite?.name ?? "NULL"} icon={icon?.name ?? "NULL"}");

        icon.sprite = sprite;
        icon.color = sprite != null ? Color.white : Color.gray;
    }


    private void OnClick() => OnCardSelected?.Invoke(_cardId, _price);
}
