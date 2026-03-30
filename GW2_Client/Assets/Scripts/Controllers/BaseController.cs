using Google.Protobuf.Enum;
using Google.Protobuf.Protocol;
using Google.Protobuf.Struct;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseController : MonoBehaviour
{
    // === Attack Particle ===
    [SerializeField] private ParticleSystem _attackEffect;

    // === Service Dependency Injection ===
    protected IInputService _inputService;
    protected INetworkService _networkService;

    // === Server Pos ===
    public int Id { get; set; }
    public float _sendPacketDelay = 0.2f;
    public float LastServerTime { get; set; }
    public bool _isMoving = false;
    public float _positionSmoothTime = 0.1f;
    public float _moveSpeed = 100.0f;
    public float _rotationSpeed = 1.0f;

    // === Server Navigation ===
    protected List<Vector3> _path = new List<Vector3>();
    protected int _pathIndex = 0;
    protected float _correctionSpeed = 1.0f;
    protected float _correctionThreshold = 1.0f;

    protected Animator _animator;

    PosInfo _posInfo = new PosInfo();

    // 진영 구분용 enum
    public CampType _campType = new CampType();

    public PosInfo PosInfo
    {
        get { return _posInfo; }
        set
        {
            if (value == null)
                return;

            _posInfo.X = value.X;
            _posInfo.Y = value.Y;
            _posInfo.Z = value.Z;

            // State setter를 통해 UpdateAnimation()까지 호출
            State = value.State;
        }
    }

    protected WUI_HpBar _hpBar;
    protected UI_StatusBar _statusBar;

    protected float _hp = 100.0f;
    protected float _maxHp = 100.0f;

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

        //_animator = GetComponent<Animator>();
        //_baseLayer = _animator.GetLayerIndex("BaseLayer");
        //_lowerLayer = _animator.GetLayerIndex("LowerLayer");

        //_animator.SetLayerWeight(_baseLayer, 1f);
        //_animator.SetLayerWeight(_lowerLayer, 1f);
        _hpBar = GetComponentInChildren<WUI_HpBar>();
        Debug.Log($"[BaseController.Init] _hpBar={(_hpBar != null ? "found" : "NULL")} on {gameObject.name}");
        if (_hpBar != null)
            _hpBar.SetHp(_hp, _maxHp);
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
            //return;
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
                //_animator.CrossFade("Idle", 0.1f, _baseLayer);
                //_animator.CrossFade("Idle", 0.1f, _lowerLayer);
                UpdateIdle();
                break;
            case MoveState.Run:
                //_animator.CrossFade("Moving", 0.1f, _baseLayer);
                //_animator.CrossFade("Moving", 0.1f, _lowerLayer);
                UpdateMoving();
                break;
            case MoveState.Skill:
                //_animator.CrossFade("SKILL", 0.1f);
                break;
            case MoveState.None:
                //_animator.CrossFade("Idle", 0.1f, _baseLayer);
                //_animator.CrossFade("Idle", 0.1f, _lowerLayer);
                break;
        }
    }

    protected int GetClientTime()
    {
        // ms단위로 전달
        return (int)(Time.realtimeSinceStartup * 1000);
    }

    public virtual void SetHp(float current, float max)
    {
        _hp = current;
        _maxHp = max;

        if (_hpBar != null)
            _hpBar.SetHp(_hp, _maxHp);
    }

    private IEnumerator MoveEffectToTarget(Vector3 targetPos)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = Vector3.one * 0.3f;
        Destroy(sphere.GetComponent<Collider>());

        Vector3 startPos = transform.position + Vector3.up;
        sphere.transform.position = startPos;
        targetPos += Vector3.up;

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (sphere == null) yield break;
            elapsed += Time.deltaTime;
            sphere.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }

        if (sphere != null) Destroy(sphere);
    }

    // 내가 공격
    public void PlayAttackEffect(Vector3 targetPos)
    {
        StartCoroutine(MoveEffectToTarget(targetPos));
    }

    // 상대가 공격
    public void PlayAttackEffect(GameObject target)
    {
        if (target == null)
            return;

        PlayAttackEffect(target.transform.position);
    }

    public virtual void UpdateHp(long current, long max)
    {

    }

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        _moveSpeed *= multiplier;
        StartCoroutine(RevertSpeedAfter(duration, multiplier));
    }

    private IEnumerator RevertSpeedAfter(float duration, float multiplier)
    {
        yield return new WaitForSeconds(duration);
        _moveSpeed /= multiplier;
    }

    public void ApplyStun(float duration)
    {
        State = MoveState.Idle; // 이동 중지
        StartCoroutine(StunCoroutine(duration));
    }



    private IEnumerator StunCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        // 스턴 해제 — 서버가 다음 이동 패킷 보낼 때 자연히 복구됨
    }

    public void ApplyAttackSpeedBuff(float multiplier, float duration)
    {
        StartCoroutine(RevertAttackSpeedAfter(duration, multiplier));
    }

    private IEnumerator RevertAttackSpeedAfter(float duration, float multiplier)
    {
        yield return new WaitForSeconds(duration);
        // 공격 쿨타임 로직 구현 시 여기서 복원
    }

}
