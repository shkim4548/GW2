#pragma once
#include "GameLogic.h"

namespace GameMath{ struct Vector3; }
namespace Navigation {  struct GridCell;  class NavigationSystem; }
using NavPath = std::vector<GameMath::Vector3>;
constexpr float MOVE_BROADCAST_INTERVAL = 0.1f; // 100ms (10Hz), 가장 일반적인 온라인 게임 브로드캐스트 주기

class Object : public enable_shared_from_this<Object>
{
public:
	Object();
	virtual ~Object();

	int32 GetObjectId() { return _objectId; }
	Protocol::PosInfo GetPosInfo() { return _pos; }
	GameMath::Vector3 GetPosVector() const { return _posVector; }
	Protocol::StatInfo GetStatInfo() const { return _statInfo; }
	Protocol::MoveState GetMoveState() const { return _moveState; }

	void SetObjectId(int64 id) { _objectId = id; }
	void SetPosInfo(Protocol::PosInfo posInfo) { _pos = posInfo; }
	void SetMoveState(Protocol::MoveState moveState) { _moveState = moveState; }
	void SetIsMoving(bool isMoving) { _isMoving = isMoving; }

	// Navigation
	void SetPath(const NavPath& path);
	bool GetIsMoving() const { return _isMoving; }
	bool UpdateMovement(float deltaTime);
	void RequestMove(const vector<GameMath::Vector3>& path);
	void PostUpdate();
	
	// Game Room Logic
	void AccumulateMoveTime(float deltaTime);
	bool ShouldBroadcastMove() const;
	void ResetBroadcastTimer();
	bool ValidateMovement(float deltaTime);
	
public:
	NavPath _path;
	size_t  _pathIndex = 0;

protected:
	int64 _objectId = 0;
	Protocol::PosInfo _pos;
	Protocol::StatInfo _statInfo;
	GameMath::Vector3 _posVector;
	weak_ptr<Navigation::NavigationSystem> _navigationSystem;

	bool _isMoving = false;
	Protocol::MoveState _moveState = Protocol::MoveState::MOVE_STATE_IDLE;
	float _moveSpeed = 100.0f;

private:
	float _moveBroadcastElapsed = 0.0f;

	// === 이동상태 이상 탐지 === 
	GameMath::Vector3 _lastCheckPos;
	bool _hasLastCheckPos = false;
};

