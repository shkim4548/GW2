#pragma once
#include "Object.h"

class Room;

class Minion : public Object
{
public:
	Minion();
	virtual ~Minion() = default;

	// called by GameRoom
	void UpdateMinion(float deltaTime);
	
private:
	// === STATE MACHINE ===
	void UpdateIdle(float deltaTime);
	void UpdateLaneTrace(float deltaTime);
	void UpdateChaseTarget(float deltaTime);
	void UpdateAttack(float deltaTime);

	weak_ptr<Object> FindBestTarget();
	int32 GetTargetPriority(Object* obj);

public:
	uint8 _laneId;

private:
	Protocol::MinionState _minionState;

	float _moveSpeed;
	float _attackRange;
	float _detectionRange;
	float _attackCooldown;

	ObjectRef _currentTarget;

	int32 _currentWaypointIndex;
	vector<weak_ptr<Object>> _targets;
};