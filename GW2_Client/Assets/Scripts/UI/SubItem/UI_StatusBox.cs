using Google.Protobuf.Enum;
using Google.Protobuf.Struct;
using TMPro;
using UnityEngine;

public class UI_StatusBox : UI_Base
{
    enum Status
    {
        Attack_Text,
        Shield_Text,
        AttackSpeed_Text,
        Speed_Text,
        Strength_Text,
        Gold_Text
    }

    private float _attack = 0f;
    private float _shield = 0f;
    private float _attackSpeed = 1f;   // 기본 1.0 (배율)
    private float _speed = 0f;
    private float _strength = 0f;
    private long _gold = 0;

    public override void Init()
    {
        Bind<TMP_Text>(typeof(Status));

        MyPlayerController.OnStatInfoUpdate -= HandleStatInfo;
        MyPlayerController.OnStatInfoUpdate += HandleStatInfo;

        MyPlayerController.OnBuffApplied -= HandleBuffApplied;   // ← 단일 구독
        MyPlayerController.OnBuffApplied += HandleBuffApplied;

        UI_Store.OnGoldUpdate -= HandleGoldUpdate;
        UI_Store.OnGoldUpdate += HandleGoldUpdate;

        var pending = MyPlayerController.GetPendingStatInfo();
        if (pending != null)
            HandleStatInfo(pending);

        Refresh();
    }

    private void HandleStatInfo(StatInfo stat)
    {
        _attack = stat.Attack;
        _speed = stat.Speed;
        // Shield / Strength는 서버 미제공 → 유지
        Refresh();
    }

    private void HandleBuffApplied(BuffType buffType, float value)
    {
        switch (buffType)
        {
            case BuffType.BuffAttack:
                _attack *= value;       // 표시용 배율 적용
                break;
            case BuffType.BuffDefense:
                _shield *= value;       // 기존 _shield 필드 활용
                break;
            case BuffType.BuffAttackSpeed:
                _attackSpeed = value;   // 배율 직접 표시
                break;
            case BuffType.BuffSpeed:
                _speed *= value;
                break;
        }
        Refresh();
    }

    private void HandleAttackSpeedBuff(float value)
    {
        _attackSpeed = value;
        Refresh();
    }

    private void HandleGoldUpdate(long gold)
    {
        _gold = gold;
        Refresh();
    }

    private void Refresh()
    {
        GetTMP((int)Status.Attack_Text).text = _attack.ToString("F0");
        GetTMP((int)Status.Shield_Text).text = _shield.ToString("F0");
        GetTMP((int)Status.AttackSpeed_Text).text = _attackSpeed.ToString("F2");
        GetTMP((int)Status.Speed_Text).text = _speed.ToString("F0");
        GetTMP((int)Status.Strength_Text).text = _strength.ToString("F0");
        GetTMP((int)Status.Gold_Text).text = _gold.ToString();
    }


    private void OnDestroy()
    {
        MyPlayerController.OnStatInfoUpdate -= HandleStatInfo;
        MyPlayerController.OnAttackSpeedBuffed -= HandleAttackSpeedBuff;
        UI_Store.OnGoldUpdate -= HandleGoldUpdate;
    }

}
