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

	// called by GameRoom
	void UpdateMinion(float deltaTime);
	
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
	const weak_ptr<Navigation::LaneRoute> _route;
};