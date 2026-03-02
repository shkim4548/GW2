using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionController : BaseController
{
    [SerializeField] private float _lerpSpeed = 10.0f;     // 보간 속도
    [SerializeField] private float _snapDistance = 1.0f;   // 너무 멀면 스냅

    public override void Init()
    {
        base.Init();
        Debug.Log("Minion Controller Init");
    }

    public override void UpdateIdle()
    {
        base.UpdateIdle();
        Debug.Log("Minion UpdateIdle");
    }

    public override void UpdateMoving()
    {
        base.UpdateMoving();
        Debug.Log("Minion State Update Moving");
        Vector3 current = transform.position;
        Vector3 serverPosVector = new Vector3(PosInfo.X, PosInfo.Y, PosInfo.Z);

        float dist = Vector3.Distance(current, serverPosVector);

        // 스냅이 필요한경우
        if(dist > _snapDistance)
        {
            transform.position = serverPosVector;
        }

        // 평상시 -> 부드럽게 보간해준다.
        if(dist > 0.01f)
        {
            transform.position = Vector3.MoveTowards(current, serverPosVector, _lerpSpeed * Time.deltaTime);
        }
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
