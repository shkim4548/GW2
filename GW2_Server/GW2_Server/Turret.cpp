#include "pch.h"
#include "Room.h"
#include "Turret.h"
#include "StatLoader.h"
#include "Lobby.h"

Turret::Turret()
{
	_objectType = Protocol::OBJECT_TYPE_TURRET;
	_statInfo.set_hp(3000);
	_statInfo.set_max_hp(3000);
	_statInfo.set_attack(150);
}

Turret::~Turret()
{
}

void Turret::InitTurret(shared_ptr<Room> room, Protocol::CampType team)
{
	_room = room;
	if (_room.lock() == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"Turret Room is nullptr\n");
	}
	_teamId = static_cast<uint8>(team);
	_campType = team;

	UnitStat stat = GLobby->GetUnitStat("turret");
	if (stat.hp > 0)
	{
		_statInfo.set_hp(stat.hp);
		_statInfo.set_max_hp(stat.maxHp);
		_statInfo.set_attack(stat.attackDamage);
		_attackInterval = stat.attackInterval;
		_attackRangeSq = stat.attackRange * stat.attackRange;
	}
}

void Turret::UpdateController(float deltaTime)
{
	Update(deltaTime);
}

void Turret::Update(float deltaTime)
{
	// 타이머 갱신
	if (_aggroTimer > 0.0f)
	{
		_aggroTimer -= deltaTime;
		if (_aggroTimer <= 0.0f)
		{
			_aggroTarget.reset();
			_aggroTimer = 0.0f;
		}
	}

	// 현재 타겟 유효성 검사 -> 무효면 해제한다
	shared_ptr<Object> currentTarget = _currentTarget.lock();
	if (currentTarget && !IsValidTarget(currentTarget))
	{
		_currentTarget.reset();
		currentTarget = nullptr;
	}

	// 어그로 타겟이 있으면 강제 전환
	shared_ptr<Object> aggroTarget = _aggroTarget.lock();
	if (aggroTarget && IsValidTarget(aggroTarget))
	{
		if (_currentTarget.lock() != aggroTarget)
		{
			_currentTarget = aggroTarget;
			currentTarget = aggroTarget;
		}
	}

	// 타겟 없으면 새로 탐색한다.
	if (currentTarget == nullptr)
	{
		currentTarget = AcquireTarget().lock();
		if (currentTarget)
		{
			_currentTarget = currentTarget;
		}
	}

	// 타겟 있으면 공격 쿨타임 처리
	if (currentTarget)
	{
		_attackCoolDown -= deltaTime;
		if (_attackCoolDown <= 0.0f)
		{
			FireCall(currentTarget);
			_attackCoolDown = _attackInterval;
		}
	}
}

bool Turret::IsValidTarget(shared_ptr<Object> target) const
{
	if (target == nullptr || target->IsDead())
	{
		return false;
	}

	// 팀 체크: 같은 팀이면 무효 타겟
	if (target->GetTeamFlag() == _campType)
	{
		return false;
	}

	const GameMath::Vector3& tThisPos = GetPosVector();
	const GameMath::Vector3& tTargetPos = target->GetPosVector();
	float distSq = GetDistanceSq(tThisPos, tTargetPos);
	return distSq <= _attackRangeSq;
}

void Turret::FireCall(shared_ptr<Object> target)
{
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
		return;

	uint64 dmg = _statInfo.attack();
	bool died = target->ApplyDamage(dmg);

	room->DoAsync(&Room::HandleTurretAttack, this->GetObjectId(), target->GetObjectId());
	if (died)
	{
		target->OnDead();
	}
}

weak_ptr<Object> Turret::AcquireTarget()
{
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
	{
		return weak_ptr<Object>();
	}

	shared_ptr<Object> bestTarget = nullptr;
	float bestDistSq = FLT_MAX;
	int32 bestPriority = -1;

	// 우선순위 : 플레이어 -> 미니언
	// 어그로 없을 때는 미니언 우선
	for (auto& [id, obj] : room->GetRoomObjects())
	{
		if (obj == nullptr || obj->IsDead())
		{
			continue;
		}

		// 아군은 타겟해선 안된다
		if (obj->GetTeamFlag() == _teamId)
		{
			continue;
		}

		if (IsValidTarget(obj) == false)
		{
			continue;
		}

		if (obj->GetTeamFlag() == _campType)
		{
			continue;
		}

		// 우선순위를 만들어준다.
		int32 priority = 0;
		if (obj->GetObjectType() == Protocol::OBJECT_TYPE_MINION)
		{
			priority = 1;
		}
		else if (obj->GetObjectType() == Protocol::OBJECT_TYPE_PLAYER)
		{
			priority = 2;
		}
		else
		{
			continue;
		}

		float distSq = GetDistanceSq(GetPosVector(), obj->GetPosVector());

		// 최우선 타겟 return
		if (obj->GetObjectType() == Protocol::OBJECT_TYPE_MINION)
		{
			if (bestTarget == nullptr ||
				bestTarget->GetObjectType() == Protocol::OBJECT_TYPE_PLAYER ||
				distSq < bestDistSq)
			{
				bestTarget = obj;
				bestDistSq = distSq;
				bestPriority = priority;
			}
		}
		else if (obj->GetObjectType() == Protocol::OBJECT_TYPE_PLAYER)
		{
			// 미니언 타겟이 없을 때만 플레이어 선택
			if (bestTarget == nullptr)
			{
				bestTarget = obj;
				bestDistSq = distSq;
				bestPriority = priority;
			}
		}
	}
	return bestTarget;
}

float Turret::GetDistanceSq(const GameMath::Vector3& a, const GameMath::Vector3& b) const
{
	float dx = a._x - b._x;
	float dz = a._z - b._z;
	return dx * dx + dz * dz;
}

void Turret::OnDead()
{
	shared_ptr<Room> room = _room.lock();
	if (!room) return;

	room->DoAsync(&Room::HandleRemoveObject, GetObjectId(), -1);
}
