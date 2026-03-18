using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WUI_HpBar : UI_Base
{
    enum Images
    {
        HealthBar
    }
    private Transform _target;
    private Vector3 _offset = Vector3.up * 2.0f;
    public override void Init()
    {
        Bind<Image>(typeof(Images));
    }

    private void LateUpdate()
    {
        if (Camera.main == null)
            return;

        transform.LookAt(Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void SetHp(float current, float max)
    {
        if (max <= 0f) 
            return;

        Image hpImage = GetImage((int)Images.HealthBar);
        if (hpImage == null) return;

        hpImage.fillAmount = Mathf.Clamp01(current / max);
    }
}
