using Google.Protobuf.Enum;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class MyPlayerController : PlayerController
{
    float _speed = 10.0f;

    Vector3 _destPos;
    bool _moveToDest = false;

    GameObject _target;
    NavMeshAgent _navAgent;

    public override void Init()
    {
        base.Init();
        // CameraController 바인딩
        FindObjectOfType<CameraController>().SetPlayer(this);

        _navAgent = GetComponent<NavMeshAgent>();
        _inputService = DI.Container.Resolve<IInputService>();

        _inputService.MouseAction -= OnMouseEvent;
        _inputService.MouseAction += OnMouseEvent;

        _inputService.KeyAction -= OnKeyEvent;
        _inputService.KeyAction += OnKeyEvent;
    }

    public override void UpdateIdle()
    {
        base.UpdateIdle();
    }

    public override void UpdateMoving()
    {
        base.UpdateMoving();
        if (_moveToDest)
        {
            Vector3 dir = _destPos - transform.position;
            //Debug.Log(dir.magnitude);
            if (dir.magnitude < 0.1f)
            {
                _moveToDest = false;
                State = MoveState.Idle;
            }
            else
            {
                float moveDist = Mathf.Clamp(_speed * Time.deltaTime, 0, dir.magnitude);
                //transform.position += dir.normalized * moveDist;
                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 20 * Time.deltaTime);
                _navAgent.SetDestination(_destPos);
                //Debug.Log(_destPos);
            }
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

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Road")))
        {
            _destPos = hit.point;
            _moveToDest = true;
            //UpdateMoving();
            State = MoveState.Run;
            // 상태 변화 확인
            //Debug.Log(State);
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

            }
        }
    }

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
}
