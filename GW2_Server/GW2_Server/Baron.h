#pragma once
#include "Object.h"

// 전방 선언
class Room;
namespace Navigation { struct LaneRoute; }
namespace GameMath { struct Vector3; }

class Baron : public Object
{
public:
	Baron();
	virtual ~Baron();

	void InitBaron(shared_ptr<Room> room, GameMath::Vector3 spawnPos);
	void SetBaronId(int32 id) { _objectInfo.set_object_id(id); }
	int32 GetBaronId() { return _objectInfo.object_id(); }
	float GetMoveSpeed() { return _moveSpeed; }
	float GetDetectionRange() { return _detectionRange; }


	void SetLaneRoute(shared_ptr<Navigation::LaneRoute> route);
	void ClearPathPending() { _pathPending = false; }

	void OnHit(int32 attackerId);

	// virtuals
	virtual void UpdateController(float deltaTime) override;
	virtual void UpdateMovement(float deltaTime) override;

	void RequestMove(vector<GameMath::Vector3> path);
protected:
	virtual void OnDead() override;

private:
	void UpdateIdle(float deltaTime);
	//void UpdatePatrol(float deltaTime);
	void UpdateCombat(float deltaTime);

	shared_ptr<Object> SelectTarget();
	bool HasLivingAggroTarget();
	void ResetBaron();

public:
	// MID LINE(중앙 공원)
	uint8 _laneId = 2;

private:
	Protocol::BaronState _baronState = Protocol::BaronState::BARON_IDLE;
	float _attackRange = 4.0f;
	float _detectionRange = 12.0f;
	float _attackCooldown = 0.0f;
	float _attackInterval = 1.5f;
	float _leashRange = 20.0f;   // 스폰 위치로부터 최대 추격 거리

	// 스킬 타이머 (타겟 있을 때만 감소)
	float _skill1Timer = 8.0f;    // AOE 슬램
	float _skill2Timer = 15.0f;   // 독장판 (즉시 데미지)
	static constexpr float SKILL1_INTERVAL = 8.0f;
	static constexpr float SKILL1_RANGE = 5.0f;
	static constexpr float SKILL1_DAMAGE = 120.0f;
	static constexpr float SKILL2_INTERVAL = 15.0f;
	static constexpr float SKILL2_RANGE = 6.0f;
	static constexpr float SKILL2_DAMAGE = 150.0f;

	// aggro: objectId → 마지막 피격 시각
	unordered_map<int32, float> _aggroTable;
	float _aggroElapsed = 0.0f;  // 타겟 없을 때 누적 시간
	static constexpr float NO_TARGET_RESET = 10.0f;

	weak_ptr<Object> _currentTarget;

	// 순찰
	shared_ptr<Navigation::LaneRoute> _route;
	int32 _currentWaypointIndex = 0;
	bool  _patrolForward = true;

	GameMath::Vector3 _lastMoveGoal;
	float _repathCoolDown = 0.0f;
	bool  _pathPending = false;

	GameMath::Vector3 _spawnPos;
	GameMath::Vector3 _destPos;

	// 보상 카드 풀
	static const vector<int32> REWARD_CARDS;
};

