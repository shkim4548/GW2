using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : CreatureController
{
    private float interpolationSpeed = 10.0f;
    [SerializeField] private float _remoteSpeed = 12.0f;

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
        Vector3 serverPos = new Vector3(PosInfo.X, transform.position.y, PosInfo.Z);
        // snap 조건 제거 - 목적지는 멀 수 있음
        transform.position = Vector3.MoveTowards(
            transform.position, serverPos, _remoteSpeed * Time.deltaTime);

        Vector3 direction = new Vector3(PosInfo.X - transform.position.x, 0f, PosInfo.Z - transform.position.z);
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
        }
        else
        {
            // 목적지에 거의 도달 → 서버 yaw로 최종 회전 정렬
            Quaternion targetRotation = Quaternion.Euler(0f, PosInfo.Yaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
        }
    }

}
