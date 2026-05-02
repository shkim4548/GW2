using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : CreatureController
{
    private float interpolationSpeed = 10.0f;
    [SerializeField] private float _remoteSpeed = 12.0f;

    public override void Init()
    {
        base.Init();
    }

    // Idle 상태에서도 서버 Yaw를 반영
    public override void UpdateIdle()
    {
        base.UpdateIdle();
        Quaternion targetRot = Quaternion.Euler(0f, PosInfo.Yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, interpolationSpeed * Time.deltaTime);
    }

    public override void UpdateMoving()
    {
        base.UpdateMoving();
        if (_isMoving)
        {
            InterpolateToServerPosition();
        }
    }

    // Remote 플레이어 위치 보간
    private void InterpolateToServerPosition()
    {
        float groundY = GetGroundY(PosInfo.X, PosInfo.Z);
        Vector3 serverPos = new Vector3(PosInfo.X, groundY, PosInfo.Z);

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
            Quaternion targetRotation = Quaternion.Euler(0f, PosInfo.Yaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
        }
    }

    // NavMesh 기반 지형 Y 보정 (언덕 등 고저차 처리)
    private float GetGroundY(float x, float z)
    {
        NavMeshHit hit;
        // 현재 위치 기준 위아래 5f 범위 내에서 NavMesh 샘플링
        Vector3 samplePos = new Vector3(x, transform.position.y + 5f, z);
        if (NavMesh.SamplePosition(samplePos, out hit, 10f, NavMesh.AllAreas))
            return hit.position.y;

        // NavMesh 샘플링 실패 시 현재 Y 유지 (안전 fallback)
        return transform.position.y;
    }
}
