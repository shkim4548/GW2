#pragma once
#include "Object.h"

class Turret : public Object
{
public:
	void Init();
	Turret();
	virtual ~Turret();
	void InitTurret(shared_ptr<Room> room, Protocol::CampType team);

	// Getters
	uint8 GetTeamId() const { return _teamId; }

	// Setters
	void SetTeamId(uint8 teamId) { _teamId = teamId; }
	void SetTurretId(int32 turretId) { _objectInfo.set_object_id(turretId); }

private:
	virtual void UpdateController(float deltaTime) override;
	void Update(float deltaTime);
	bool IsValidTarget(shared_ptr<Object> target) const;
	void FireCall(shared_ptr<Object> target);
	weak_ptr<Object> AcquireTarget();
	float GetDistanceSq(const GameMath::Vector3& a, const GameMath::Vector3& b) const;

protected:
	virtual void OnDead() override;

private:
	weak_ptr<Object> _currentTarget;
	weak_ptr<Object> _aggroTarget;
	float _aggroTimer = 0.0f;

	// 스탯
	float _attackRange = 8.0f;
	float _attackInterval = 1.2f;
	float _attackCoolDown = 0.1f;
	static constexpr float AGGRO_DURATION = 6.0f;   // 어그로 유지 시간(초)
	//static constexpr float ATTACK_RANGE_SQ = 64.0f;  // 8.0f * 8.0f
	float _attackRangeSq = 64.0f;

	uint8 _teamId = 0;
};