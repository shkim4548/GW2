using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_CardPanel : UI_Base
{
    public static Action<List<int>> OnHandSync;
    public static Action<int> OnDrawCard;

    private static UI_CardPanel _instance;
    private List<int> _handCardIds = new List<int>();

    enum GameObjects { Q, W, E, R }

    public override void Init()
    {
        _instance = this;
        Bind<GameObject>(typeof(GameObjects));

        OnHandSync -= HandleHandSync;
        OnHandSync += HandleHandSync;
        OnDrawCard -= HandleDrawCard;
        OnDrawCard += HandleDrawCard;
        MyPlayerController.OnCardUsed -= HandleCardUsed;
        MyPlayerController.OnCardUsed += HandleCardUsed;

        RefreshUI();
    }

    private void HandleHandSync(List<int> cardIds)
    {
        _handCardIds = cardIds;
        RefreshUI();
    }

    private void HandleDrawCard(int cardId)
    {
        _handCardIds.Add(cardId);
        RefreshUI();
    }

    private void HandleCardUsed(int slotIndex)
    {
        if (slotIndex >= _handCardIds.Count) return;
        _handCardIds.RemoveAt(slotIndex);
        RefreshUI();
    }

    public static int GetCardIdAtSlot(int slotIndex)
    {
        if (_instance == null) return -1;
        if (slotIndex >= _instance._handCardIds.Count) return -1;
        return _instance._handCardIds[slotIndex];
    }

    private void RefreshUI()
    {
        string[] keyLabels = { "Q", "W", "E", "R" };

        for (int i = 0; i < 4; i++)
        {
            Transform slot = GetObject(i).transform;

            foreach (Transform child in slot)
                Destroy(child.gameObject);

            if (i >= _handCardIds.Count) continue;

            GameObject cardObj = Bootstrapper.Instance.ResourceService
                .Instantiate("UI/SubItem/UI_CardSlot", slot);
            UI_CardSlot cardSlot = cardObj.GetComponent<UI_CardSlot>();
            cardSlot.Init();
            cardSlot.SetHandMode(_handCardIds[i], keyLabels[i]);

        }
    }

    private void OnDestroy()
    {
        _instance = null;
        OnHandSync -= HandleHandSync;
        OnDrawCard -= HandleDrawCard;
        MyPlayerController.OnCardUsed -= HandleCardUsed;
    }
}
