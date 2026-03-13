#include "pch.h"
#include "Room.h"
#include "Player.h"

Player::Player()
{
    _objectType = Protocol::ObjectType::OBJECT_TYPE_PLAYER;
}

Player::~Player()
{

}

void Player::InitPlayer()
{

}

void Player::UpdateController(float deltaTime)
{
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
    float remainMoveDist = _moveSpeed * deltaTime;

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

void Player::OnDead()
{
    // TODO : 플레이어에게 보상 지급
    shared_ptr<Room> room = _room.lock();
    if (room == nullptr)
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::OnDead] room is nullptr\n");
        return;
    }

    room->DoAsync(&Room::HandleRemoveObject, _objectId);
}
