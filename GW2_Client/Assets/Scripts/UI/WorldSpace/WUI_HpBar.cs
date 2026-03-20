using UnityEngine;
using UnityEngine.UI;

public class WUI_HpBar : UI_Base
{
    enum Images { HealthBar }

    void Awake()  // Start() 대신 Awake()에서 미리 바인딩
    {
        Init();
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
    }

    private void LateUpdate()
    {
        if (Camera.main == null) 
            return;
        
        transform.rotation = Camera.main.transform.rotation;
    }

    public void SetHp(float current, float max)
    {
        if (max <= 0f) return;
        Image hpImage = GetImage((int)Images.HealthBar);
        if (hpImage == null) return;
        hpImage.fillAmount = Mathf.Clamp01(current / max);
    }
}
