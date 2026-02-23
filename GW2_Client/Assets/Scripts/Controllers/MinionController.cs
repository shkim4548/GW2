using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionController : BaseController
{
    public override void Init()
    {
        base.Init();
    }

    public override void UpdateIdle()
    {
        base.UpdateIdle();
    }

    public override void UpdateMoving()
    {
        base.UpdateMoving();
        Debug.Log("Minion State Update Moving");
    }

    public override void UpdateSkill()
    {
        base.UpdateSkill();
    }

    public override void UpdateDead()
    {
        base.UpdateDead();
    }
}
