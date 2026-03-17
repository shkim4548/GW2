using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StatusBox : UI_Base
{
    // UI에 보여주기 위한 컨테이너
    Google.Protobuf.Struct.StatInfo _statInfo;

    enum Status
    {
        Attack_Text,
        Shield_Text,
        AttackSpeed_Text,
        Speed_Text,
        Strength_Text,
        Gold_Text
    }

    public override void Init()
    {
        _statInfo = new Google.Protobuf.Struct.StatInfo();
    }
}
