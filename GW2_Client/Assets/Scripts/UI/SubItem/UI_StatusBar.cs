using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_StatusBar : UI_Base
{
    enum Images { HealthBar }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
    }

    public void SetHp(float current, float max)
    {
        if (max <= 0f) return;
        GetImage((int)Images.HealthBar).fillAmount = Mathf.Clamp01(current / max);
    }
}
