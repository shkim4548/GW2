using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionController : BaseController
{
    // ★ FIX 1: _moveSpeed 중복 선언 제거
    // BaseController에 public float _moveSpeed = 100.0f 가 이미 존재함.
    // 여기서 재선언하면 private 필드가 BaseController.field를 shadow하여
    // SetMoveSpeed()가 저장한 값(100 유닛/초)으로 UpdateMoving()이 동작해 텔레포트가 발생함.
    // → 아래 _moveSpeed 선언을 제거하고 BaseController의 _moveSpeed를 직접 사용.

    [SerializeField] private float _arriveEpsilon = 0.05f;  // waypoint 도착 판정 거리
    [SerializeField] private float _snapDistance = 5.0f;    // 서버 스냅 보정 거리 임계값

    // ★ FIX 2: 보정 파라미터 Inspector 기본값 수정
    // BaseController._correctionThreshold = 1.0f → 너무 작아 Lerp가 항상 발동.
    // 미니언은 서버가 먼저 이동하므로 threshold를 넉넉하게 잡아야 진동이 없음.
    // Unity Inspector에서 아래 값으로 조정 (혹은 Start()에서 초기화).
    // _correctionThreshold = 2.5f  (서버와 클라 사이 허용 오차)
    // _correctionSpeed     = 5.0f  (Lerp 속도)

    // navPath 작성용
    private List<Vector3> _navPath = new List<Vector3>();
    private int _navIndex = 0;
    private bool _hasPath = false;

    // 서버 위치 참조값
    private Vector3 _serverRefPos;
    private int _serverRefTime;
    private bool _hasServerRef = false;

    public override void Init()
    {
        base.Init();

        // ★ FIX 2 (보완): Inspector에서 건드리지 않았을 경우 코드로 안전값 세팅
        // BaseController 기본값 _correctionThreshold=1.0f, _correctionSpeed=1.0f 를 덮어씀
        if (_correctionThreshold < 2.0f)
            _correctionThreshold = 2.5f;
        if (_correctionSpeed < 2.0f)
            _correctionSpeed = 5.0f;

        Debug.Log("[MinionController] Init: correctionThreshold=" + _correctionThreshold
                  + " correctionSpeed=" + _correctionSpeed
                  + " moveSpeed=" + _moveSpeed);
    }

    public override void UpdateIdle()
    {
        SetMoveAnim(false);
        base.UpdateIdle();
    }

    public override void UpdateMoving()
    {
        if (!_hasPath || _navPath.Count == 0)
        {
            SetMoveAnim(false);
            return;
        }
        SetMoveAnim(true);       // ← 추가

        // 경로 끝에 도달했으면 정지
        if (_navIndex >= _navPath.Count)
        {
            ArriveAtDestination();
            return;
        }

        // ★ FIX 3: 서버 보정은 이동 후에 적용 (이동 전 적용 시 매 프레임 앞으로 당겨져 진동)
        // 또한 이동 중에는 보정을 완전히 비활성화하고, S_MOVE_END 후에만 최종 위치 맞춤
        // → ApplyServerCorrection()을 이동 후로 이동 + 이동 중 Lerp 비활성화

        // ── 매 프레임 남은 이동거리를 소비하면서 waypoint를 순서대로 통과 ──
        float remainDist = _moveSpeed * Time.deltaTime;

        while (remainDist > 0f && _navIndex < _navPath.Count)
        {
            Vector3 target = _navPath[_navIndex];
            Vector3 toTarget = target - transform.position;
            float dist = toTarget.magnitude;

            if (dist <= _arriveEpsilon)
            {
                // waypoint 도착 → 다음 waypoint로
                transform.position = target;
                _navIndex++;
                continue;
            }

            if (dist <= remainDist)
            {
                // 이번 프레임에 waypoint 통과 가능
                transform.position = target;
                remainDist -= dist;
                _navIndex++;
            }
            else
            {
                // 이번 프레임에 waypoint까지 도달 불가 → 방향으로 이동
                transform.position += toTarget.normalized * remainDist;
                remainDist = 0f;

                // 회전 — 이동 방향을 바라봄 (Y축 회전만)
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
                }
            }
        }

        // ★ FIX 3: 이동 완료 후 서버 보정 적용 (스냅 전용, Lerp는 이동 중 제거)
        ApplyServerCorrection();

        // 경로 끝에 도달했으면 정지
        if (_navIndex >= _navPath.Count)
            ArriveAtDestination();
    }

    public override void UpdateSkill()
    {
        base.UpdateSkill();
    }

    public override void UpdateDead()
    {
        base.UpdateDead();
    }

    /// <summary>
    /// S_MOVE로 받은 _serverRefPos 와 현재 클라이언트 위치를 비교해 오차를 보정한다.
    ///
    /// [수정된 정책]
    ///   이동 중(navPath 추종 중)에는 스냅(_snapDistance 이상)만 허용.
    ///   Lerp 보정은 이동 중에 비활성화 — S_MOVE가 100ms마다 오므로
    ///   "서버가 클라이언트보다 항상 앞서있는" 상황에서 Lerp를 매 프레임 적용하면
    ///   앞으로 당겨지다가 다시 앞으로 당겨지는 진동이 발생함.
    ///   대신 오차가 snap 임계값을 넘을 때만 순간 이동하여 심각한 desync를 복구.
    ///
    /// [Inspector 튜닝 가이드]
    ///   _correctionThreshold : 사용 안 함 (이동 중 Lerp 비활성화)
    ///   _correctionSpeed     : 사용 안 함 (이동 중 Lerp 비활성화)
    ///   _snapDistance        : 4.0 ~ 6.0  (ping 스파이크 대응)
    /// </summary>
    private void ApplyServerCorrection()
    {
        if (!_hasServerRef || !_hasPath)
            return;

        float err = Vector3.Distance(transform.position, _serverRefPos);

        if (err >= _snapDistance)
        {
            // 오차가 snap 임계값 이상 → 즉시 스냅 후 navIndex 재조정
            transform.position = _serverRefPos;
            // ★ FIX 4: FindNearestWaypointIndex → FindNearestForwardWaypointIndex
            // 이미 지나간 waypoint로 역주행하지 않도록 현재 navIndex 이후 waypoint만 검색
            _navIndex = FindNearestForwardWaypointIndex(_serverRefPos, _navPath, _navIndex);
            Debug.LogWarning($"[MinionController] SNAP to serverPos err={err:F2} newNavIdx={_navIndex}");
        }
        // else: 이동 중 Lerp 보정 제거 — 정상 오차는 무시

        _hasServerRef = false;
    }

    // -----------------
    //  Setters
    // -----------------
    public virtual void SetServerRefPos(Vector3 serverPos, int serverTime)
    {
        _serverRefPos = serverPos;
        _serverRefTime = serverTime;
        _hasServerRef = true;
    }

    // NavPath Helper

    public virtual void SetNavPath(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            StopNavPath();
            return;
        }

        _navPath = path;
        _hasPath = true;
        _isMoving = true;
        _hasServerRef = false; // ★ 이전 S_MOVE 보정값 초기화 (새 경로 시작)

        // ★ FIX 5: snap 판정은 path[0] 기준이 아니라 현재 위치 기준으로만
        // path[0] = 서버의 미니언 현재 위치이므로 클라이언트와 일치해야 정상.
        // snap이 필요하다면 snapDistance보다 클 때만 보정.
        float distToFirst = Vector3.Distance(transform.position, path[0]);
        if (distToFirst > _snapDistance)
        {
            transform.position = path[0];
            _navIndex = 1;
            Debug.LogWarning($"[MinionController] SetNavPath: snap to path[0] dist={distToFirst:F2}");
        }
        else
        {
            // ★ FIX 4: 현재 위치 기준으로 가장 가까운 waypoint에서 시작
            // path[0]은 미니언 현재 위치이므로 index 0이 아닌 1부터 시작하는 것이 맞음
            _navIndex = 1; // path[0]은 현재 위치이므로 skip
        }

        // State를 Run으로 세팅 (Start() 이전에도 유효하도록 직접 세팅)
        PosInfo.State = Google.Protobuf.Enum.MoveState.Run;

        //Debug.Log($"[MinionController] SetNavPath count={path.Count} startIdx={_navIndex} moveSpeed={_moveSpeed:F1}");
    }

    public void SetMoveSpeed(float speed)
    {
        // ★ FIX 1 연계: BaseController._moveSpeed에 저장 (중복 필드 없으므로 직접 저장됨)
        if (speed > 0.0f)
        {
            _moveSpeed = speed;
            //Debug.Log($"[MinionController] SetMoveSpeed: {speed:F1} (유닛/초 단위 확인 필요)");
        }
    }

    // internal helper
    private void ArriveAtDestination()
    {
        _hasPath = false;
        _isMoving = false;
        _hasServerRef = false;
        SetMoveAnim(false);

        // 마지막 waypoint에 도착함
        if (_navPath.Count > 0)
            transform.position = _navPath[_navPath.Count - 1];

        // State를 Idle로 전환
        State = Google.Protobuf.Enum.MoveState.Idle;
        //Debug.Log($"[MinionController] Arrived at destination. pos={transform.position}");
    }

    private void StopNavPath()
    {
        _hasPath = false;
        _isMoving = false;
        _navPath.Clear();
        _navIndex = 0;
        _hasServerRef = false;
    }

    /// <summary>
    /// ★ FIX 4: 현재 navIndex 이후 waypoint 중 가장 가까운 것을 반환.
    /// 이미 지나간 waypoint를 선택해 역주행하는 버그를 방지.
    /// </summary>
    private int FindNearestForwardWaypointIndex(Vector3 currentPos, List<Vector3> path, int currentIndex)
    {
        // 현재 인덱스 이후부터만 탐색 (최소 1 이상)
        int searchFrom = Mathf.Max(currentIndex, 0);
        int nearest = searchFrom;
        float minDistSq = float.MaxValue;

        for (int i = searchFrom; i < path.Count; i++)
        {
            float dSq = (path[i] - currentPos).sqrMagnitude;
            if (dSq < minDistSq)
            {
                minDistSq = dSq;
                nearest = i;
            }
        }
        return nearest;
    }
    private void Attack() { }  // 애니메이션 이벤트 수신용
}