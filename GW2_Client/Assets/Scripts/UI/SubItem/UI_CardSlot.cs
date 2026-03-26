using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_CardSlot : UI_Base
{
    public static Action<int, int> OnCardSelected; // cardId, price

    enum Images { CardIcon }
    enum Texts { Key_txt, CardName_txt, Damage_txt, Price_txt }
    enum GameObjects { StoreInfo, HandInfo }

    private int _cardId;
    private int _price;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<Text>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));

        GetComponent<Button>()?.onClick.AddListener(OnClick);
    }

    // 손패 모드 (Q/W/E/R)
    public void SetHandMode(int cardId, string keyLabel)
    {
        _cardId = cardId;
        GetObject((int)GameObjects.StoreInfo).SetActive(false);
        GetObject((int)GameObjects.HandInfo).SetActive(true);
        GetText((int)Texts.Key_txt).text = keyLabel;
        LoadIcon(cardId);
    }

    // 상점 모드
    public void SetStoreMode(Data.CardInfo card)
    {
        _cardId = card.id;
        _price = card.price;
        GetObject((int)GameObjects.StoreInfo).SetActive(true);
        GetObject((int)GameObjects.HandInfo).SetActive(false);
        GetText((int)Texts.CardName_txt).text = card.name;         // skill_id → name
        GetText((int)Texts.Damage_txt).text = $"DMG {card.damage}";
        GetText((int)Texts.Price_txt).text = $"{card.price} G";
        LoadIcon(card.id);
    }


    private void LoadIcon(int cardId)
    {
        Sprite sprite = Resources.Load<Sprite>($"UI/CardIcons/Card_{cardId}");
        Image icon = GetImage((int)Images.CardIcon);
        icon.sprite = sprite;
        icon.color = sprite != null ? Color.white : Color.gray;
    }

    private void OnClick()
    {
        OnCardSelected?.Invoke(_cardId, _price);
    }
}
