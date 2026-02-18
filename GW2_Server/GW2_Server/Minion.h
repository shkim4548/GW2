#pragma once
#include "Object.h"

class Room;

namespace Navigation { struct LaneRoute; }
namespace GameMath { struct Vector3; }

class Minion : public Object
{
public:
	Minion();
	virtual ~Minion();
	void SetMinionId(int32 id) { _objectId = id; }
	int32 GetMinionId() { return _objectId; }

	// called by GameRoom
	void UpdateMinion(float deltaTime);

	// === LaneRoute Handler === 
	void SetLaneRoute(shared_ptr<Navigation::LaneRoute> route);
	weak_ptr<Navigation::LaneRoute> GetLaneRoute() const;

private:
	// === STATE MACHINE ===
	void UpdateIdle(float deltaTime);
	void UpdateLaneTrace(float deltaTime);
	void UpdateChaseTarget(float deltaTime);
	void UpdateAttack(float deltaTime);

	// === STATE MACHINE HELPER ===
	weak_ptr<Object> FindBestTarget();
	int32 GetTargetPriority(shared_ptr<Object> obj);
	bool ShouldChaseTargetNow(shared_ptr<Object> target);
	uint8 GetLaneIdFromPos(GameMath::Vector3& targetPos);


public:
	uint8 _laneId;

private:
	Protocol::MinionState _minionState;

	float _moveSpeed;
	float _attackRange;
	float _detectionRange;
	float _attackCooldown;
	float _attackInterval;

	weak_ptr<Object> _currentTarget;

	int32 _currentWaypointIndex;
	vector<weak_ptr<Object>> _targets;
	weak_ptr<Navigation::LaneRoute> _route;

	GameMath::Vector3 _lastMoveGoal = GameMath::Vector3(FLT_MAX, 0.0f, FLT_MAX);
	float _repathCoolDown = 0.0f;
};