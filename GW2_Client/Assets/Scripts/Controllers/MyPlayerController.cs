using Google.Protobuf.Enum;
using Google.Protobuf.Protocol;
using Google.Protobuf.Struct;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Define;


public class MyPlayerController : PlayerController
{
    float _speed = 10.0f;

    Vector3 _destPos;
    bool _moveToDest = false;
    private bool _pendingSkillIsTarget = false;

    GameObject _target;
    NavMeshAgent _navAgent;

    private float _attackRange = 5.0f;
    private bool _chaseToAttack = false;
    private float _chaseCooldown = 0.0f;
    private Vector3 _lastTargetPos;
    private float _attackInterval = 1.0f;
    private float _attackCooldown = 0.0f;
    private float _attackSpeedMult = 1.0f;

    private int _clientMoveStartTime;
    private bool _needsCorrection = false;

    public int RoomId { get; set; }

    // === UI Delegate ===
    public static Action<float, float> OnHpChanged;
    public static Action<int> OnCardUsed;

    private UI_Store _storeUI = null;
    private int _pendingSkillSlot = -1; // -1 = 스킬 대기 없음
    public CampType CampType { get; set; }

    // HandSync 버퍼 (UI 생성 전 패킷 도착 대비)
    private static List<int> _pendingHandCardIds = new List<int>();

    public static Action<Google.Protobuf.Struct.StatInfo> OnStatInfoUpdate;
    public static Action<float> OnAttackSpeedBuffed;

    public static void SetPendingHandSync(List<int> cardIds)
    {
        _pendingHandCardIds = new List<int>(cardIds);
    }

    public static List<int> GetPendingHandCardIds() => _pendingHandCardIds;

    public override void Init()
    {
        base.Init();
        // CameraController 바인딩
        FindObjectOfType<CameraController>().SetPlayer(this);

        _navAgent = GetComponent<NavMeshAgent>();
        _inputService = Bootstrapper.Instance.InputService;
        _networkService = Bootstrapper.Instance.NetworkService;

        _inputService.MouseAction -= OnMouseEvent;
        _inputService.MouseAction += OnMouseEvent;

        _inputService.KeyAction -= OnKeyEvent;
        _inputService.KeyAction += OnKeyEvent;

        Id = _networkService.GetNetworkId();
        //_campType = Google.Protobuf.Enum.CampType.CampHuman;

        IUIService uiService = Bootstrapper.Instance.UIService;
        uiService.ShowSceneUI<UI_GameScene>();
        
        _navAgent = GetComponent<NavMeshAgent>();
        if (_navAgent != null)
            _navAgent.updateRotation = false;  // 추가: 수동 회전 제어 사용

        if (_pendingHandCardIds.Count > 0)
            UI_CardPanel.OnHandSync?.Invoke(_pendingHandCardIds);
    }

    public override void UpdateIdle()
    {
        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;

        // 타겟이 있고 쿨다운 끝나면 재공격 시도
        if (_target != null && _attackCooldown <= 0f)
        {
            float dist = Vector3.Distance(transform.position, _target.transform.position);
            if (dist <= _attackRange)
            {
                TryAttackTarget();
            }
            else
            {
                // 타겟이 이동했으면 다시 추적
                _chaseToAttack = true;
                State = MoveState.Run;
                RequestMove(_target.transform.position);
            }
        }

        base.UpdateIdle();
    }

    public override void UpdateMoving()
    {
        //base.UpdateMoving();
        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;

        // 추적 중이면 매 프레임 타겟 거리 갱신
        if (_chaseToAttack && _target != null)
        {
            _chaseCooldown -= Time.deltaTime;
            Vector3 targetPos = _target.transform.position;
            float dist = Vector3.Distance(transform.position, targetPos);

            if (dist <= _attackRange)
            {
                TryAttackTarget();
                _chaseToAttack = false;
                StopMovement();
                return;
            }

            if (_chaseCooldown <= 0f &&
                Vector3.Distance(_lastTargetPos, targetPos) > 0.5f)
            {
                RequestMove(targetPos);
                _lastTargetPos = targetPos;
                _chaseCooldown = 0.3f;
            }
        }

        //Debug.Log("UpdateMoving");
        // 보정 필요성부터 확인
        if (_needsCorrection)
        {
            CorrectPosition();
        }

        if(_path ==  null || _path.Count == 0)
        {
            StopMovement();
            return;
        }

        if(_pathIndex >= _path.Count)
        {
            StopMovement();
            return;
        }
        Vector3 waypoint = _path[_pathIndex];
        Vector3 direction = waypoint - transform.position;
        float distance = direction.magnitude;

        // 도착 여부 체크
        if(distance < 0.001f)
        {
            transform.position = waypoint;
            _pathIndex++;

            if(_pathIndex >= _path.Count)
            {
                StopMovement();
            }
            return;
        }

        // 실제 이동
        direction.Normalize();
        float moveDistance = _moveSpeed * Time.deltaTime;

        if (moveDistance >= distance)
        {
            transform.position = waypoint;
            _pathIndex++;
        }
        else
        {
            transform.position += direction * moveDistance;
            //Debug.Log("Actual moving");
        }

        // 회전 반영
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
        }
        // 상위 함수에서 상태 변화 및 애니메이션 재생
    }

    public override void UpdateSkill()
    {
        base.UpdateSkill();
    }

    public override void UpdateDead()
    {
        base.UpdateDead();
        _chaseToAttack = false;
        _target = null;
        StopMovement();
    }

    public void OnMouseEvent(Define.MouseEvent evt)
    {
        if (State == MoveState.Die) 
            return;

        // 좌클릭시 스킬 대기
        if (evt == Define.MouseEvent.LeftClick)
        {
            // target_type=2: 좌클릭으로 대상 선택
            if (_pendingSkillSlot >= 0 && _pendingSkillIsTarget)
            {
                Ray skillRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit skillHit;
                if (Physics.Raycast(skillRay, out skillHit, 100.0f, LayerMask.GetMask("Objects")))
                {
                    BaseController bc = skillHit.collider.gameObject.GetComponent<BaseController>();
                    if (bc != null && bc._campType != _campType)
                    {
                        _target = skillHit.collider.gameObject;
                        int tId = bc.Id;
                        SendCardEvent(_pendingSkillSlot, _target.transform.position, tId);
                    }
                }
                _pendingSkillSlot = -1;
                _pendingSkillIsTarget = false;
                State = MoveState.Idle;
            }
            // target_type=1: 좌클릭 지점 선택
            else if (_pendingSkillSlot >= 0 && !_pendingSkillIsTarget)
            {
                Ray skillRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit skillHit;
                if (Physics.Raycast(skillRay, out skillHit, 100.0f,
                        LayerMask.GetMask("Road", "Objects")))
                    SendCardEvent(_pendingSkillSlot, skillHit.point);

                _pendingSkillSlot = -1;
                _pendingSkillIsTarget = false;
                State = MoveState.Idle;
            }
            return;
        }
        if (evt != Define.MouseEvent.Click)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Debug.DrawRay(Camera.main.transform.position, ray.direction * 100.0f, Color.red, 1.0f);
        // CreatureController 상속 받는 물건임을 확인시 적인지를 다시한번 판단.
        if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Objects")))
        {
            Debug.Log("Raycast Objects hit");
            // 진영이 다르고 사거리 내에 있다면 상태를 전이시킨다.
            if (hit.collider.gameObject.GetComponent<BaseController>()._campType != this._campType)
            {
                // TEMP
                _target = hit.collider.gameObject;
                float dist = Vector3.Distance(transform.position, _target.transform.position);
                Debug.Log($"TryAttackTarget before dist : {dist}, range : {_attackRange}");
                if(dist <= _attackRange)
                {
                    // 사거리 내부면 바로 공격
                    TryAttackTarget();
                    Debug.Log("TryAttackTarget");
                }
                else
                {
                    // 사거리 밖이다.
                    _chaseToAttack = true;
                    _lastTargetPos = _target.transform.position;
                    _chaseCooldown = 0.0f;
                    State = MoveState.Run;
                    RequestMove(_target.transform.position);
                    Debug.Log("TryAttackTarget else block");
                }
            }
            
        }
        else if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Road")))
        {
            _destPos = hit.point;
            _moveToDest = true;
            _chaseToAttack = false;  // 이동 명령 시 추적 취소
            _target = null;
            State = MoveState.Run;
            // 상태 변화 확인
            //Debug.Log("Raycast Road");
            RequestMove(_destPos);
        }
        // 사거리 밖에 있다면, 추적시킨다.
        else
        {
            Debug.Log("OnMouseEvent Else block");
        }
    }

    // 현재는 사용하지 않는다.
    public void OnKeyEvent()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryUseCardSlot(0);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            TryUseCardSlot(1);
        }
        else if(Input.GetKeyDown(KeyCode.E))
        {
            TryUseCardSlot(2);
        }
        else if(Input.GetKeyDown(KeyCode.R))
        {
            TryUseCardSlot(3);
        }
        else if(Input.GetKeyDown(KeyCode.B))
        {
            //IUIService uiService = Bootstrapper.Instance.UIService;
            //uiService.ShowPopupUI<UI_Store>();
            if (_storeUI != null)
            {
                Debug.Log("Close Store UI");
                Bootstrapper.Instance.UIService.ClosePopupUI(_storeUI);
                _storeUI = null;
            }
            else
            {
                Debug.Log("Show Store UI");
                _storeUI = Bootstrapper.Instance.UIService.ShowPopupUI<UI_Store>();
            }
        }
        else
        {
            //Debug.LogError($"OnKeyEvent : Invalid Key Event");
        }
    }

    private void TryUseCardSlot(int slotIndex)
    {
        int cardId = UI_CardPanel.GetCardIdAtSlot(slotIndex);
        if (cardId < 0)
        {
            Debug.LogError($"[MyPlayerController::TryUseCardSlot]invalid hand card, slotIndex : {slotIndex}, cardId : {cardId}");
            return;
        }

        if (!Bootstrapper.Instance.DataService.CardDict.TryGetValue(cardId, out Data.CardInfo cardInfo))
            return;

        switch (cardInfo.target_type)
        {
            case 0: // self — 즉시 사용
                SendCardEvent(slotIndex, transform.position);
                break;
            case 1: // point — 좌클릭 지점 대기
                _pendingSkillSlot = slotIndex;
                _pendingSkillIsTarget = false;
                State = MoveState.Skill;
                break;
            case 2: // target — 우클릭 대상 대기
                _pendingSkillSlot = slotIndex;
                _pendingSkillIsTarget = true;
                State = MoveState.Skill;
                break;
        }
    }

    // 추측항법
    // 위치 정정
    private void CorrectPosition()
    {
        Vector3 serverPosVector = new Vector3(PosInfo.X, PosInfo.Y, PosInfo.Z);
        float distance = Vector3.Distance(transform.position, serverPosVector);

        if(distance < 0.01f)
        {
            transform.position = serverPosVector;
            return;
        }

        // 보정처리
        transform.position = Vector3.Lerp(transform.position, serverPosVector, _correctionSpeed * Time.deltaTime);
    }

    private void StartMovePrediction(Vector3 destination)
    {
        Vector3 targetPosition = new Vector3(PosInfo.X, PosInfo.Y, PosInfo.Z);
        // targetPosition = destination;

        // 클라이언트에서 navmesh로 경로를 미리 예측한다.
        UnityEngine.AI.NavMeshPath navPath = new UnityEngine.AI.NavMeshPath();
        if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, destination, UnityEngine.AI.NavMesh.AllAreas, navPath))
        {
            _path = new List<Vector3>(navPath.corners);
            _pathIndex = 0;
            _isMoving = true;

            //Debug.Log($"[MyPlayer] Prediction path : {_path.Count} waypoints");
            //for(int i = 0; i < _path.Count; ++i)
            //{
            //    Debug.Log($"Path Index {i} : {_path[i]}");
            //}
        }
    }

    private void StopMovement()
    {
        _isMoving = false;
        _path.Clear();
        _pathIndex = 0;

        if (_navAgent != null)
            _navAgent.ResetPath();
        State = MoveState.Idle;
    }

    private void TryAttackTarget()
    {
        if (_target == null) 
            return;
        if (_attackCooldown > 0f) 
            return;

        BaseController targetBc = _target.GetComponent<BaseController>();
        if (targetBc == null) 
            return;

        Vector3 targetPos = _target.transform.position;
        PlayAttackEffect(targetPos);

        // 기존 코루틴 중단 후 재시작 (중복 방지)
        StopCoroutine("ResetAttackAnim");
        SetAttackAnim(true);
        StartCoroutine(ResetAttackAnim(_attackInterval));  // 1.0f 고정 → _attackInterval로 변경

        SendAttackPacket(targetBc.Id);
        _attackCooldown = _attackInterval / _attackSpeedMult;
    }


    private void SendAttackPacket(int targetId)
    {
        C_SKILL attackPkt = new C_SKILL();
        attackPkt.RoomId = RoomId;
        attackPkt.AttackerId = Id;
        attackPkt.TargetId = targetId;
        attackPkt.CommandId = 1;
        attackPkt.ClientTime = GetClientTime();
        _networkService.Send(attackPkt);
        Debug.Log($"[MyPlayer] SendAttackPacket targetId={targetId}");
    }


    public void RequestMove(Vector3 worldPosition)
    {
        // 즉시 이동 시작
        StartMovePrediction(worldPosition);
        // 서버에 전송
        SendMovePacket(worldPosition);
    }

    private void SendMovePacket(Vector3 nowPosition)
    {
        C_MOVE movePacket = new C_MOVE();
        movePacket.StartPos = new PosInfo();
        movePacket.RoomId = RoomId;
        movePacket.ObjectId = Id;

        // Start Position
        movePacket.StartPos.X = transform.position.x;
        movePacket.StartPos.Y = transform.position.y;
        movePacket.StartPos.Z = transform.position.z;

        // TargetPosition
        movePacket.TargetPos = new PosInfo();
        movePacket.TargetPos.X = nowPosition.x;
        movePacket.TargetPos.Y = nowPosition.y;
        movePacket.TargetPos.Z = nowPosition.z;

        // clientTime
        movePacket.ClientTime = GetClientTime();
        _clientMoveStartTime = movePacket.ClientTime;

        _networkService.Send(movePacket);
        //Debug.Log($"movePkt destPos, startPos: {nowPosition}, {this.transform.position}");
    }

    public override void SetHp(float current, float max)
    {
        base.SetHp(current, max);
        OnHpChanged?.Invoke(current, max);
    }

    private void SendCardEvent(int slotIndex, Vector3 worldPos, int targetId = 0)
    {
        int cardId = UI_CardPanel.GetCardIdAtSlot(slotIndex);
        if (cardId < 0) 
            return;

        SetSkillAnim(true);                         //
        StartCoroutine(ResetSkillAnim(1.5f));       // (클립 길이에 맞게 조정)

        // 타겟 있으면 ID, 없으면 0 (논타겟)
        //int targetId = (_target != null) ? _target.GetComponent<BaseController>().Id : 0;

        C_SKILL pkt = new C_SKILL();
        pkt.RoomId = RoomId;
        pkt.AttackerId = Id;
        pkt.TargetId = targetId;   // 0 = 논타겟, 0 아님 = 타겟팅
        pkt.CommandId = cardId;
        pkt.ClientTime = GetClientTime();
        pkt.PosX = worldPos.x;
        pkt.PosZ = worldPos.z;
        Vector3 forward = transform.forward;
        pkt.DirX = forward.x;
        pkt.DirZ = forward.z;
        _networkService.Send(pkt);

        // 수정 후
        Vector3 toClick = (worldPos - transform.position);
        toClick.y = 0f;
        Vector3 dir = toClick.sqrMagnitude > 0.001f ? toClick.normalized : transform.forward;
        PlaySkillEffect(cardId, transform.position, worldPos, dir);

        OnCardUsed?.Invoke(slotIndex);
    }

    public void ApplyAttackSpeedBuff(float multiplier, float duration)
    {
        _attackSpeedMult = multiplier;
        StartCoroutine(RevertAttackSpeedAfter(duration));
    }

    private IEnumerator RevertAttackSpeedAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        _attackSpeedMult = 1.0f;
    }
}
