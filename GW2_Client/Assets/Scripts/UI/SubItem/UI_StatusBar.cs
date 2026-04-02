using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StatusBar : UI_Base
{
    private float _currentFill;
    private float _targetFill;

    private float _delayTimer = 0f;
    private const float DELAY_BEFORE_SHRINK = 0.5f;  // 노란바 대기 시간
    private const float SHRINK_SPEED = 0.8f;          // 노란바 따라가는 속도

    enum Images 
    {
        Hp_Fill_Red,
        Hp_Fill_Yellow,
        Mana_Fill0,
        Mana_Fill1,
        Mana_Fill2,
        Exp_Fill,
        Icon
    }

    enum Texts
    {
        NowHPText,
        MaxHPText,
        Level,
        RespawnTime
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));

        MyPlayerController.OnHpChanged -= SetHp;
        MyPlayerController.OnHpChanged += SetHp;
    }

    public void SetHp(float current, float max)
    {
        if (max <= 0f) 
            return;

        _targetFill = Mathf.Clamp01(current / max);

        // 빨간바는 즉시 갱신
        GetImage((int)Images.Hp_Fill_Red).fillAmount = _targetFill;

        // 노란바는 현재값 유지 후 대기 시작
        _delayTimer = DELAY_BEFORE_SHRINK;
        
        GetTMP((int)Texts.NowHPText).text = Mathf.RoundToInt(current).ToString();
        GetTMP((int)Texts.MaxHPText).text = Mathf.RoundToInt(max).ToString();
    }

    public void Update()
    {
        float delayFill = GetImage((int)Images.Hp_Fill_Yellow).fillAmount;

        if (delayFill <= _targetFill)
        {
            // HP 회복 시 노란바도 즉시 따라감
            GetImage((int)Images.Hp_Fill_Yellow).fillAmount = _targetFill;
            return;
        }

        if (_delayTimer > 0f)
        {
            _delayTimer -= Time.deltaTime;
            return;
        }

        // 대기 후 노란바가 빨간바를 향해 줄어듦
        GetImage((int)Images.Hp_Fill_Yellow).fillAmount =
            Mathf.MoveTowards(delayFill, _targetFill, SHRINK_SPEED * Time.deltaTime);
    }
}
