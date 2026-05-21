#include "pch.h"
#include "Room.h"
#include "Player.h"
#include "StatLoader.h"
#include "Lobby.h"

Player::Player()
{
    //_objectType = Protocol::ObjectType::OBJECT_TYPE_PLAYER;
    SetObjectType(Protocol::ObjectType::OBJECT_TYPE_PLAYER);
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
    Protocol::StatInfo* tStatInfo = _objectInfo.mutable_stat_info();
    if (stat.hp > 0)
    {
        tStatInfo->set_hp(stat.hp);
        tStatInfo->set_max_hp(stat.maxHp);
        tStatInfo->set_attack(stat.attackDamage);
        tStatInfo->set_attack_range(stat.attackRange);
        tStatInfo->set_speed(stat.moveSpeed);
        _moveSpeed = stat.moveSpeed;
        _attackInterval = stat.attackInterval;
    }
    else
    {
        // fallback
        tStatInfo->set_hp(1000);
        tStatInfo->set_max_hp(1000);
        tStatInfo->set_attack(30);
        tStatInfo->set_attack_range(15.0f);
        tStatInfo->set_speed(12.0f);   // ← fallback 속도 추가
        _moveSpeed = 12.0f;            // ← _moveSpeed 동기화

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

    if (_pathIndex >= static_cast<int32>(_path.size()))
    {
        //_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
        SetMoveState(Protocol::MoveState::MOVE_STATE_IDLE);
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
        _posVector = _posVector + dir * remainMoveDist;
        Protocol::PosInfo* tPos = _objectInfo.mutable_pos_info();
        tPos->set_x(_posVector._x);
        tPos->set_y(_posVector._y);
        tPos->set_z(_posVector._z);
        _movement.speed = 0.0f;
        return;

    }

    // while 루프 끝나고, 아래 if 블록 바로 전에 삽입:
    {
        Protocol::PosInfo* tPos = _objectInfo.mutable_pos_info();
        tPos->set_x(_posVector._x);
        tPos->set_y(_posVector._y);
        tPos->set_z(_posVector._z);
    }

    if (_pathIndex >= static_cast<int32>(_path.size()))
    {
        //_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
        SetMoveState(Protocol::MoveState::MOVE_STATE_IDLE);
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

    int32 objectId = _objectInfo.object_id();
    room->DoAsync(&Room::HandleRemoveObject, objectId, -1);
}
