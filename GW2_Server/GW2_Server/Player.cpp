#include "pch.h"
#include "Room.h"
#include "Player.h"
#include "StatLoader.h"
#include "Lobby.h"

Player::Player()
{
    _objectType = Protocol::ObjectType::OBJECT_TYPE_PLAYER;
}

Player::~Player()
{

}

void Player::InitPlayer(shared_ptr<Room> room)
{
    _room = room;
    string statKey;

    switch (_playerType)
    {
    case Protocol::PLAYER_TYPE_POLICE:      statKey = "Police";      break;
    case Protocol::PLAYER_TYPE_MONK:        statKey = "Monk";        break;
    case Protocol::PLAYER_TYPE_LIGHTSABRE:  statKey = "Lightsabre";  break;
    case Protocol::PLAYER_TYPE_FIREFIGHTER: statKey = "Firefighter"; break;
    default:                                statKey = "Police";      break;
    }

    UnitStat stat = GLobby->GetUnitStat(statKey);
    if (stat.hp > 0)
    {
        _statInfo.set_hp(stat.hp);
        _statInfo.set_max_hp(stat.maxHp);
        _statInfo.set_attack(stat.attackDamage);
        _statInfo.set_attack_range(stat.attackRange);
        _statInfo.set_speed(stat.moveSpeed);
        _attackInterval = stat.attackInterval;
    }
    else
    {
        // fallback
        _statInfo.set_hp(1000);
        _statInfo.set_max_hp(1000);
        _statInfo.set_attack(30);
        _statInfo.set_attack_range(15.0f);
    }

    //GConsoleLogger->WriteStdOut(Color::GREEN, L"[InitPlayer] hp=%llu atk=%llu\n", _statInfo.hp(), _statInfo.attack());
    
    _gold = 500;  // 초기 골드
    _cardManager.InitDeck(*this);
}

void Player::UpdateController(float deltaTime)
{
    // --- 스턴 타이머 ---
    if (_stunTimer > 0.0f)
    {
        _stunTimer -= deltaTime;
        if (_stunTimer <= 0.0f)
            _isStunned = false;
        return; // 이동/공격 전부 차단
    }

    // ── 버프 타이머 ──
    if (_attackBuffTimer > 0.0f) 
    {
        _attackBuffTimer -= deltaTime;
        if (_attackBuffTimer <= 0.0f) 
        {
            _attackMult = 1.0f; _attackBuffTimer = 0.0f;
        }
    }

    if (_defenseBuffTimer > 0.0f) 
    {
        _defenseBuffTimer -= deltaTime;
        if (_defenseBuffTimer <= 0.0f) 
        {
            _defenseReduct = 0.0f; _defenseBuffTimer = 0.0f;
        }
    }

    if (_speedBuffTimer > 0.0f) 
    {
        _speedBuffTimer -= deltaTime;
        if (_speedBuffTimer <= 0.0f) 
        {
            _speedMult = 1.0f; _speedBuffTimer = 0.0f;
        }
    }

    if (_attackSpeedBuffTimer > 0.0f)
    {
        _attackSpeedBuffTimer -= deltaTime;
        if (_attackSpeedBuffTimer <= 0.0f)
        {
            _attackSpeedMult = 1.0f; _attackSpeedBuffTimer = 0.0f;
        }
    }

    if (_attackCooldown > 0.0f)
        _attackCooldown -= deltaTime;

    if (_moveState != Protocol::MoveState::MOVE_STATE_RUN)
    {
        _movement.speed = 0.f;
        return;
    }

    if (_pathIndex >= static_cast<int32>(_path.size()))
    {
        _moveState = Protocol::MoveState::MOVE_STATE_IDLE;
        _isMoving = false;
        _movement.speed = 0.f;
        return;
    }

    const float kArriveEpsilon = 1e-4f;
    float remainMoveDist = _moveSpeed * _speedMult * deltaTime;

    while (remainMoveDist > 0.0f && _pathIndex < static_cast<int32>(_path.size()))
    {
        const GameMath::Vector3& target = _path[_pathIndex];
        GameMath::Vector3 dir = target - _posVector;
        const float dist = dir.Length();

        if (dist <= kArriveEpsilon)
        {
            _posVector = target;
            ++_pathIndex;
            continue;
        }

        if (dist <= remainMoveDist)
        {
            _posVector = target;
            remainMoveDist -= dist;
            ++_pathIndex;
            continue;
        }

        dir = dir.Normalized();
        _movement.direction = dir;
        _movement.speed = _moveSpeed;

        // 여기서 remainMoveDist만큼 이동하는 건 Movement에게 맡김
        // (혹은 remainMoveDist를 고려해서 speed 설정)
        return;
    }

    if (_pathIndex >= static_cast<int32>(_path.size()))
    {
        _moveState = Protocol::MoveState::MOVE_STATE_IDLE;
        _isMoving = false;
        _movement.speed = 0.f;
    }
}

void Player::AddCardToDeck(int32 cardId)
{
}

void Player::OnDead()
{
    // TODO : 플레이어에게 보상 지급
    shared_ptr<Room> room = _room.lock();
    if (room == nullptr)
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::OnDead] room is nullptr\n");
        return;
    }

    room->DoAsync(&Room::HandleRemoveObject, _objectId, -1);
}
