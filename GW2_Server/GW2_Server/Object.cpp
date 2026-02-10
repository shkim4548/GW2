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
	if (_moveState != Protocol::MoveState::MOVE_STATE_RUN)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Object UpdateMovement] invalid state : move_state_run\n");
		return false;
	}

	// 경로 유효성 체크, 유효하지 않다면 IDLE로 바꾸고 return
	if (_pathIndex >= static_cast<int32>(_path.size()))
	{
		_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
		return false;
	}

	const float kArriveEpsilon = 1e-4f;
	float remainMoveDist = _moveSpeed * deltaTime;
	bool moved = false;

	// _path를 소모하는 루프를 이용, 한틱만 움직이는 현상을 방지한다.
	while (remainMoveDist > 0.0f && _pathIndex < _path.size())
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
			moved = true;
		}
		else
		{
			dir = dir.Normalized();
			_posVector = _posVector + (dir * remainMoveDist);
			++_pathIndex;
			moved = true;
		}
	}

	if (_pathIndex >= _path.size())
	{
		_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
	}

	_pos.set_x(_posVector._x);
	_pos.set_y(_posVector._y);
	_pos.set_z(_posVector._z);
	cout << "posInfo :" << _pos.x() << ' ' << _pos.y() << ' ' << _pos.z() << endl;
	cout << "_posVector : " << _posVector._x << ' ' << _posVector._y << ' ' << _posVector._z << endl;
	return moved;
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
