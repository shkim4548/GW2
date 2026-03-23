using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_CardPanel : UI_Base
{
    public static Action<List<int>> OnHandSync;
    public static Action<int> OnDrawCard;

    private static UI_CardPanel _instance;

    private List<int> _handCardIds = new List<int>();

    enum Images { Q, W, E, R }

    public override void Init()
    {
        _instance = this;

        Bind<Image>(typeof(Images));

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
        for (int i = 0; i < 4; i++)
        {
            Image slot = GetImage(i);
            if (slot == null) continue;

            if (i < _handCardIds.Count)
            {
                slot.enabled = true;
                // 카드 ID별 색상 구분 (임시, 추후 스프라이트로 교체)
                slot.color = GetCardColor(_handCardIds[i]);
            }
            else
            {
                slot.enabled = false;
            }
        }
    }

    private Color GetCardColor(int cardId)
    {
        // 임시: 카드 ID에 따라 색상 구분
        float t = Mathf.InverseLerp(2, 11, cardId);
        return Color.Lerp(Color.cyan, Color.red, t);
    }

    private void OnDestroy()
    {
        _instance = null;
        OnHandSync -= HandleHandSync;
        OnDrawCard -= HandleDrawCard;
        MyPlayerController.OnCardUsed -= HandleCardUsed;
    }
}
