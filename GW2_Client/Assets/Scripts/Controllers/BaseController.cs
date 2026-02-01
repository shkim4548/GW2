using Google.Protobuf.Enum;
using Google.Protobuf.Protocol;
using Google.Protobuf.Struct;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseController : MonoBehaviour
{
    protected IInputService _inputService;
    protected INetworkService _networkService;

    public int Id { get; set; }
    public float _sendPacketDelay = 0.2f;
    public float LastServerTime { get; set; }
    public bool _isMoving = false;

    protected Animator _animator;

    PosInfo _posInfo = new PosInfo();

    // 진영 구분용 enum
    public CampType _campType = new CampType();

    public PosInfo PosInfo
    {
        get { return _posInfo; }
        set
        {
            if (_posInfo != value)
                return;

            State = value.State;

            // 3차원 좌표 설정
            PosInfo.X = value.X;
            PosInfo.Y = value.Y;
            PosInfo.Z = value.Z;
        }
    }

    public virtual MoveState State
    {
        get { return PosInfo.State; }
        set
        {
            if (PosInfo.State == value)
                return;

            PosInfo.State = value;
            UpdateAnimation();
        }
    }

    private int _baseLayer;
    private int _lowerLayer;

    public void Start()
    {
        //_inputService = DI.Container.Resolve<IInputService>();
        _inputService = Bootstrapper.Instance.InputService;
        Init();
    }

    public void Update()
    {
        _inputService.OnUpdate();
        // 상태머신 관리
        UpdateAnimation();
    }


    public virtual void Init()
    {

        _animator = GetComponent<Animator>();
        _baseLayer = _animator.GetLayerIndex("BaseLayer");
        _lowerLayer = _animator.GetLayerIndex("LowerLayer");

        _animator.SetLayerWeight(_baseLayer, 1f);
        _animator.SetLayerWeight(_lowerLayer, 1f);

    }

    public virtual void UpdateIdle() { }
    public virtual void UpdateMoving() { }
    public virtual void UpdateSkill() { }
    public virtual void UpdateDead() { }
    public virtual void UpdateAnimation() 
    {
        if (_animator == null)
        {
            Debug.Log("Animator is null");
            return;
        }

        // Protobuf의 Enum을 사용
        // node, fadeTime, layerIndex
        //Debug.Log($"State in UpdateAnimation {State}");
        switch (State)
        {
            case MoveState.Die:
                UpdateDead();
                break;
            case MoveState.Idle:
                Debug.Log("Idle");
                _animator.CrossFade("Idle", 0.1f, _baseLayer);
                _animator.CrossFade("Idle", 0.1f, _lowerLayer);
                UpdateIdle();
                break;
            case MoveState.Run:
                _animator.CrossFade("Moving", 0.1f, _baseLayer);
                _animator.CrossFade("Moving", 0.1f, _lowerLayer);
                UpdateMoving();
                break;
            case MoveState.Skill:
                _animator.CrossFade("SKILL", 0.1f);
                break;
            case MoveState.None:
                _animator.CrossFade("Idle", 0.1f, _baseLayer);
                _animator.CrossFade("Idle", 0.1f, _lowerLayer);
                break;
        }
    }

    protected virtual void MakeSendPacket(float delay)
    {
    }

    protected int GetClientTime()
    {
        // ms단위로 전달
        return (int)(Time.realtimeSinceStartup * 1000);
    }
}
