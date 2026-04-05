using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : CreatureController
{
    private float interpolationSpeed = 10.0f;

    private Vector3 _serverPosition;
    private Vector3 _velocity;

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
        if(_isMoving)
        {
            InterpolateToServerPosition();
        }
        // update animation은 Base에서 진행해준다.
    }

    // === Remote 보간 이동 === 
    private void InterpolateToServerPosition()
    {
        Vector3 serverPos = new Vector3(PosInfo.X, PosInfo.Y, PosInfo.Z);

        transform.position = Vector3.SmoothDamp(
            transform.position, serverPos, ref _velocity, _positionSmoothTime);

        Vector3 direction = serverPos - transform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
        }
    }
}
