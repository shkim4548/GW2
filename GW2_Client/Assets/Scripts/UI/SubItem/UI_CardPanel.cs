using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_CardPanel : UI_Base
{
    enum Slots
    {
        Q,
        W,
        E,
        R
    }

    public override void Init()
    {
        Bind<Button>(typeof(Slots));

        GetButton((int)Slots.Q).gameObject.BindEvent(HandleQ);
        GetButton((int)Slots.W).gameObject.BindEvent(HandleW);
        GetButton((int)Slots.E).gameObject.BindEvent(HandleE);
        GetButton((int)Slots.R).gameObject.BindEvent(HandleR);
    }

    public void HandleQ(PointerEventData data)
    {

    }

    public void HandleW(PointerEventData data)
    {

    }

    public void HandleE(PointerEventData data)
    {

    }

    public void HandleR(PointerEventData data)
    {

    }
}
