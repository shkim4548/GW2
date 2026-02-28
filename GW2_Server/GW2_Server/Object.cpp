#include "pch.h"
#include "Object.h"
#include "NavigationSystem.h"

Object::Object()
{
}

Object::~Object()
{
}

bool Object::IsDead()
{
	return false;
}

void Object::SetPosInfo(Protocol::PosInfo posInfo)
{
	_posVector = GameMath::Vector3(posInfo.x(), posInfo.y(), posInfo.z());
	_pos = posInfo;
}

void Object::SetPath(const NavPath& path)
{
	if (path.empty())
	{
		_isMoving = false;
		_path.clear();
		_pathIndex = 0;
		return;
	}

	_path = path;
	_pathIndex = 0;
	_isMoving = true;
}

void Object::UpdateMovement(float deltaTime)
{
	if (_movement.speed <= 0.f)
		return;

	GameMath::Vector3 delta = _movement.direction * _movement.speed * deltaTime;
	_posVector = _posVector + delta;

	_pos.set_x(_posVector._x);
	_pos.set_y(_posVector._y);
	_pos.set_z(_posVector._z);
}

void Object::RequestMove(const vector<GameMath::Vector3>& path)
{
	// TODO : 여기부터 다시보도록 하겠다에 언급된 부분부터 다시봐야함
	if (path.empty())
	{
		_isMoving = false;
		_path.clear();
		_pathIndex = 0;
		return;
	}

	_path = path;
	_pathIndex = 0;
	_isMoving = true;
}

void Object::PostUpdate()
{
	_isMoving = (_moveState == Protocol::MOVE_STATE_RUN);
}

void Object::UpdateController(float deltaTime)
{
	cout << "Object::UpdateController" << endl;
}

void Object::AccumulateMoveTime(float deltaTime)
{
	// 이동 중일 때만 누적시킨다. 이 숫자를 보고 너무 자주 보내서 네트워크 터지지 않게한다.
	if (_moveState == Protocol::MOVE_STATE_RUN)
	{
		_moveBroadcastElapsed += deltaTime;
	}
}

bool Object::ShouldBroadcastMove() const
{
	// 아직 브로드캐스트 주기에 도달하지 않았다.
	if (_moveBroadcastElapsed < MOVE_BROADCAST_INTERVAL)
	{
		return false;
	}

	// 이동 중일 때만 flag true
	return (_moveState == Protocol::MOVE_STATE_RUN);
}

void Object::ResetBroadcastTimer()
{
	_moveBroadcastElapsed = 0.0f;
}

bool Object::ValidateMovement(float deltaTime)
{
	// 아직 기준점이 없다 -> 현재 위치를 초기 기준으로 삼고 통과한다.
	if (_hasLastCheckPos == false)
	{

	}
	return false;
}
