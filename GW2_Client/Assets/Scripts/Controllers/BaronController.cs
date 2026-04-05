using Google.Protobuf.Enum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaronController : MinionController
{
    public override void Init()
    {
        base.Init();
        _campType = CampType.CampNeutural;
    }
}
