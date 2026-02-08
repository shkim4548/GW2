using Google.Protobuf.Enum;
using Google.Protobuf.Protocol;
using Google.Protobuf.Struct;
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

    GameObject _target;
    NavMeshAgent _navAgent;

    private int _clientMoveStartTime;
    private bool _needsCorrection = false;

    public int RoomId { get; set; }

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
    }

    public override void UpdateIdle()
    {
        base.UpdateIdle();
    }

    public override void UpdateMoving()
    {
        base.UpdateMoving();
        //Debug.Log("UpdateMoving");
        // 보정 필요성부터 확인
        if(_needsCorrection)
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
        if(distance < 0.1f)
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
        }

        // 회전 반영
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
        // 상위 함수에서 상태 변화 및 애니메이션 재생
    }

    public override void UpdateSkill()
    {
        base.UpdateSkill();
    }

    public void OnMouseEvent(Define.MouseEvent evt)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(Camera.main.transform.position, ray.direction * 100.0f, Color.red, 1.0f);
        Debug.Log("OnMouseEvent");
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Road")))
        {
            _destPos = hit.point;
            _moveToDest = true;
            //UpdateMoving();
            State = MoveState.Run;
            // 상태 변화 확인
            //Debug.Log(State);
            RequestMove(_destPos);
        }
        // CreatureController 상속 받는 물건임을 확인시 적인지를 다시한번 판단.
        else if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Creature")))
        {
            // 진영이 다르고 사거리 내에 있다면 상태를 전이시킨다.
            if (hit.collider.gameObject.GetComponent<BaseController>()._campType != this._campType)
            {

            }
            // 사거리 밖에 있다면, 추적시킨다.
            else
            {
                Debug.Log("OnMouseEvent Else block");
            }
        }
    }

    // 현재는 사용하지 않는다.
    public void OnKeyEvent()
    {
        if (Input.GetKey(KeyCode.Q))
        {

        }
        else if (Input.GetKey(KeyCode.W))
        {

        }
        else if(Input.GetKey(KeyCode.E))
        {

        }
        else if(Input.GetKey(KeyCode.R))
        {

        }
        else
        {
            //Debug.LogError($"OnKeyEvent : Invalid Key Event");
        }
    }

    protected override void MakeSendPacket(float delay)
    {
        //movePacket.Info.Yaw = this.transform.rotation;
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

            Debug.Log($"[MyPlayer] Prediction path : {_path.Count} waypoints");
        }
    }

    private void StopMovement()
    {
        _isMoving = false;
        _path.Clear();
        _pathIndex = 0;
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
        movePacket.TargetPos.X = _destPos.x;
        movePacket.TargetPos.Y = _destPos.y;
        movePacket.TargetPos.Z = _destPos.z;

        // clientTime
        movePacket.ClientTime = GetClientTime();
        _clientMoveStartTime = movePacket.ClientTime;

        _networkService.Send(movePacket);
        Debug.Log($"movePkt destPos, startPos: {_destPos}, {this.transform.position}");
    }
}
