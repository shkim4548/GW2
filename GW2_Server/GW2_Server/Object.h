#pragma once
#include "GameLogic.h"

namespace GameMath{ struct Vector3; }
namespace Navigation {  struct GridCell;  class NavigationSystem; }
using NavPath = std::vector<GameMath::Vector3>;

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

	// Navigation
	void SetPath(const NavPath& path);
	bool GetIsMoving() const { return _isMoving; }
	bool UpdateMovement(float deltaTime);
	void RequestMove(const vector<GameMath::Vector3>& path);
	
	// Astar
	
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

	Protocol::MoveState _moveState;

	Protocol::MoveState _moveState = MoveState::Idle;
	float _moveSpeed = 5.f;
};

