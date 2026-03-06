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
	void InitMinion();
	void SetMinionId(int32 id) { _objectId = id; }
	int32 GetMinionId() { return _objectId; }
	uint8 GetLaneId() { return _laneId; }
	int32 GetCurrentWaypointIndex() { return _currentWaypointIndex; }
	float GetMoveSpeed() { return _moveSpeed; }

	void SetMinionTarget(vector<weak_ptr<Object>>& targets);
	void SetMinionLaneId(uint8 laneId) { _laneId = laneId; }
	weak_ptr<Object> FindBestTarget(vector<weak_ptr<Object>> targets);
	void ClearPathPending() { _pathPending = false; 	}

	// called by GameRoom
	virtual void UpdateController(float deltaTime) override;
	virtual void UpdateMovement(float deltaTime) override;

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
	bool RequestFindTarget();
	int32 GetTargetPriority(shared_ptr<Object> obj);
	bool ShouldChaseTargetNow(shared_ptr<Object> target);
	uint8 GetLaneIdFromPos(GameMath::Vector3& targetPos);


public:
	uint8 _laneId;

private:
	Protocol::MinionState _minionState;

	float _moveSpeed = 10.0f;
	float _attackRange;
	float _detectionRange =  5.0f;
	float _attackCooldown;
	float _attackInterval;

	weak_ptr<Object> _currentTarget;

	int32 _currentWaypointIndex = 0;
	// CRITICAL SECTION! RETURNED BY ROOM THREAD! CRITICAL!
	vector<weak_ptr<Object>> _targets;
	weak_ptr<Object> _bestTarget;
	weak_ptr<Navigation::LaneRoute> _route;

	GameMath::Vector3 _lastMoveGoal = GameMath::Vector3(FLT_MAX, 0.0f, FLT_MAX);
	float _repathCoolDown = 0.0f;
	bool _pathPending = false;  // DoAsync 요청 후 RequestMove 완료 전까지 중복 요청 방지
};