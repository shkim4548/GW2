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

void Object::SetPosVector(GameMath::Vector3& posVector)
{
	_posVector = GameMath::Vector3(posVector._x, posVector._y, posVector._z);
	Protocol::PosInfo tPos;
	tPos.set_x(posVector._x);
	tPos.set_y(posVector._y);
	tPos.set_z(posVector._z);
	_pos = tPos;
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
		_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
		return;
	}

	_path = path;
	_pathIndex = 0;
	_isMoving = true;
	_moveState = Protocol::MoveState::MOVE_STATE_RUN;
}

void Object::RequestMoveFrom(const vector<GameMath::Vector3>& path, int32 startIndex)
{
	if (path.empty())
	{
		_isMoving = false;
		_path.clear();
		_pathIndex = 0;
		_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
		return;
	}

	_path = path;
	// 현재 위치와 가장 가까운 지점부터 이동 시작 (뒤로 돌아가지 않음)
	_pathIndex = max(0, min(startIndex, static_cast<int32>(path.size()) - 1));
	_isMoving = true;
	_moveState = Protocol::MoveState::MOVE_STATE_RUN;
}

void Object::PostUpdate()
{
	//_isMoving = (_moveState == Protocol::MOVE_STATE_RUN);
}

void Object::UpdateController(float deltaTime)
{
	//cout << "Object::UpdateController" << endl;
}

void Object::MarkForceBroadcastMove()
{
	// 다음 UpdateRoom 루프에서 바로 브로드캐스트되도록 한다.
	_forceBroadcastMove = true;
	// 시간 조건도 만족시켜야한다.
	_moveBroadcastElapsed = MOVE_BROADCAST_INTERVAL;
}

void Object::OnMoveBroadcastSent()
{
	_forceBroadcastMove = false;
	_moveBroadcastElapsed = 0.0f;
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
	// 평상시에는 기존 조건 유지
	if (!_forceBroadcastMove && _moveBroadcastElapsed < MOVE_BROADCAST_INTERVAL)
	{
		return false;
	}

	// 이동 중이거나, 강제 브로드캐스트가 요청된 상태라면 true
	if (_moveState == Protocol::MoveState::MOVE_STATE_RUN)
		return true;

	if (_forceBroadcastMove)
		return true;

	return false;
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
