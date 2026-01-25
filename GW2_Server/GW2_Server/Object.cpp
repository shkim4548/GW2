#include "pch.h"
#include "Object.h"
#include "NavigationSystem.h"

Object::Object()
{
}

Object::~Object()
{
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

bool Object::UpdateMovement(float deltaTime)
{
	// 상태부터 우선 체크한다.
	if (_moveState != MoveState::Moving)
	{
		return false;
	}

	// 경로 유효성 체크
	if (_pathIndex >= static_cast<int32>(_path.size()))
	{
		_moveState = MoveState::Idle;
		return false;
	}

	const GameMath::Vector3& target = _path[_pathIndex];
	GameMath::Vector3 dir = target - _posVector;

	float dist = dir.Length();
	float moveDist = _moveSpeed * deltaTime;

	if (dist <= moveDist)
	{
		//_pos = target;
		_posVector = target;
		++_pathIndex;
	}
	else
	{
		dir = dir.Normalized();
		_posVector = _posVector + (dir * moveDist);
	}

	return true;
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
