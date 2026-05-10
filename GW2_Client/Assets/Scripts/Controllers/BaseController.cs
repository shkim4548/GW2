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
    public int RoomId { get; set; }
    public Vector3 SpawnPosition { get; set; }
    public bool IsAlive { get; private set; } = true;
    public void SetAlive(bool alive)
    {
        IsAlive = alive;
        SetBodyActive(alive);
        if (alive)
        {
            _isMoving = false;
            PosInfo.State = MoveState.Idle;
        }
    }

    public float _sendPacketDelay = 0.2f;
    public float LastServerTime { get; set; }
    public bool _isMoving = false;
    public float _positionSmoothTime = 0.1f;
    protected float _moveSpeed = 12.0f;
    public float _rotationSpeed = 1.0f;

    // === Server Navigation ===
    protected List<Vector3> _path = new List<Vector3>();
    protected int _pathIndex = 0;
    protected float _correctionSpeed = 1.0f;
    protected float _correctionThreshold = 1.0f;

    protected Animator _animator;
    public CampType CampType { get; set; }

    PosInfo _posInfo = new PosInfo();

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
            _posInfo.Yaw = value.Yaw;
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

            // Die에서 다른 상태로 전환 시 IsDead 자동 해제
            if (PosInfo.State == MoveState.Die && value != MoveState.Die)
                _animator?.SetBool("IsDead", false);

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
        //_inputService.OnUpdate();
        // 상태머신 관리
        UpdateAnimation();
    }

    public virtual void Init()
    {
        _animator = GetComponent<Animator>();
        _hpBar = GetComponentInChildren<WUI_HpBar>();
        //Id = Bootstrapper.Instance.NetworkService.GetNetworkId();
        //Debug.Log($"[BaseController.Init] _hpBar={(_hpBar != null ? "found" : "NULL")} on {gameObject.name}");
        if (_hpBar != null)
            _hpBar.SetHp(_hp, _maxHp);

        if (_animator != null)
            _animator.applyRootMotion = false;
    }

    public virtual void UpdateIdle() { }
    public virtual void UpdateMoving() { }
    public virtual void UpdateSkill() { }
    public virtual void UpdateDead() { }
    public virtual void UpdateAnimation() 
    {
        if (_animator == null)
        {
            Debug.LogError("Animator is null");
            return;
        }

        // Protobuf의 Enum을 사용
        // node, fadeTime, layerIndex
        //Debug.Log($"State in UpdateAnimation {State}");
        switch (State)
        {
            case MoveState.Die:
                SetDeadAnim();       
                UpdateDead();
                break;
            case MoveState.Idle:
                SetMoveAnim(false);  
                UpdateIdle();
                break;
            case MoveState.Run:
                SetMoveAnim(true);   
                UpdateMoving();
                break;
            case MoveState.Skill:
                SetMoveAnim(false); // _isMoving 초기화
                UpdateSkill();
                break;
            case MoveState.None:
                SetMoveAnim(false);                
                break;
        }
    }

    public IEnumerator ResetAttackAnim(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetAttackAnim(false);
        if (State == MoveState.Skill)
            State = MoveState.Idle;   // ← 공격 애니메이션 끝나면 Idle 복귀
    }

    public IEnumerator ResetSkillAnim(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetSkillAnim(false);
        if (State == MoveState.Skill)       
            State = MoveState.Idle;         
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
    public virtual void ApplyBuff(BuffType buffType, float value, float duration)
    {
        switch (buffType)
        {
            case BuffType.BuffSpeed:
                _moveSpeed *= value;
                StartCoroutine(RevertBuffAfter(duration, () => _moveSpeed /= value));
                break;
            case BuffType.BuffAttack:
            case BuffType.BuffDefense:
            case BuffType.BuffAttackSpeed:
                // 기본 구현 없음 — 서버가 실제 계산 담당
                // MyPlayerController에서 필요 시 override
                break;
        }
    }

    protected IEnumerator RevertBuffAfter(float duration, System.Action revert)
    {
        yield return new WaitForSeconds(duration);
        revert?.Invoke();
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

    private IEnumerator RevertAttackSpeedAfter(float duration, float multiplier)
    {
        yield return new WaitForSeconds(duration);
        // 공격 쿨타임 로직 구현 시 여기서 복원
    }

    // 카드 스킬 이펙트
    public void PlaySkillEffect(int skillId, Vector3 attackerPos, Vector3 worldPos, Vector3 dir)
    {
        if (EffectService.Instance == null) 
            return;
        EffectService.Instance.SpawnEffect(skillId, attackerPos, worldPos, dir, transform);
    }

    // 이동 상태 반영 — UpdateMoving/UpdateIdle에서 호출
    protected void SetMoveAnim(bool isMoving)
    {
        _animator?.SetBool("IsMoving", isMoving);
    }

    // 공격 시작/종료
    public void SetAttackAnim(bool on)
    {
        _animator?.SetBool("IsAttack", on);
    }

    // 스킬 시작/종료 (미니언은 파라미터 없으므로 자동 무시)
    public void SetSkillAnim(bool on)
    {
        _animator?.SetBool("IsSkill", on);
    }

    // 사망
    public void SetDeadAnim()
    {
        _animator?.SetBool("IsDead", true);
    }

    // Die 시 메시 숨기기 / Respawn 시 메시 표시
    public void SetBodyActive(bool active)
    {
        Transform meshRoot = transform.Find("CharacterMesh");
        if (meshRoot != null)
        {
            meshRoot.gameObject.SetActive(active);
        }
        else
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = active;
        }
    }

}
