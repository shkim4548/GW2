#include "pch.h"
#include "Minion.h"
#include "Room.h"
#include "NavigationSystem.h"

Minion::Minion()
{
}

Minion::~Minion()
{
}

void Minion::UpdateMinion(float deltaTime)
{
	switch (_minionState)
	{
	case Protocol::MinionState::MINION_IDLE:
		UpdateIdle(deltaTime);
		break;
	case Protocol::MinionState::MINION_LINE_TRACE:
		UpdateLaneTrace(deltaTime);
		break;
	case Protocol::MinionState::MINION_CHASE_TARGET:
		UpdateChaseTarget(deltaTime);
		break;
	case Protocol::MinionState::MINION_ATTACK:
		UpdateAttack(deltaTime);
		break;
	case Protocol::MinionState::MINION_DEAD:
		// TODO : 미니언 제거 후 보상 지급
		break;
	}
}

void Minion::UpdateIdle(float deltaTime)
{
	if (IsDead())
	{
		_minionState = Protocol::MinionState::MINION_DEAD;
		return;
	}

	// 공격할 타겟 있는지 확인
	shared_ptr<Object> target = FindBestTarget().lock();
	shared_ptr<Object> currentTarget = _currentTarget.lock();
	if (target != nullptr)
	{
		currentTarget = target;
		_minionState = Protocol::MinionState::MINION_CHASE_TARGET;
		return;
	}

	// 아직 안갔던 waypoint가 남아있으면 이동 시작
	shared_ptr<Navigation::LaneRoute> route = _route.lock();
	if (route != nullptr)
	{
		_minionState = Protocol::MinionState::MINION_LINE_TRACE;
		return;
	}
	// 이동 포인트에 종점이 있으면 state 종료
}

void Minion::UpdateLaneTrace(float deltaTime)
{
	GameMath::Vector3 myPos = GetPosVector();

	// 타겟 탐색
	shared_ptr<Object> target = FindBestTarget().lock();
	if (target != nullptr)
	{
		// 플레이어가 라인 안에 있어도 waypoint가 더 가깝다면 waypoint로 이동한다.
		if (ShouldChaseTargetNow(target))
		{
			_currentTarget = target;
			_minionState = Protocol::MinionState::MINION_CHASE_TARGET;
			return;
		}

		// false면 계속해서 라인을 따라간다.
	}

	// waypoint 따라 이동한다
	shared_ptr<Navigation::LaneRoute> route = _route.lock();
	if (route == nullptr || route->waypoints.empty())
	{
		_minionState = Protocol::MinionState::MINION_IDLE;
		return;
	}
	if(_currentWaypointIndex < 0 || _currentWaypointIndex >= static_cast<int32>(route->waypoints.empty()))
	{ 
		_currentWaypointIndex = 0;
	}

	GameMath::Vector3 nowWp = route->waypoints[_currentWaypointIndex];

	float distToNextPoint = GameMath::Vector3::GetDistTanceXZ(myPos, nowWp);

	// 도착 판정
	if (distToNextPoint < 0.3f)
	{
		if (_currentWaypointIndex + 1 < static_cast<int32>(route->waypoints.size()))
		{
			++_currentWaypointIndex;
			nowWp = route->waypoints[_currentWaypointIndex];
		}
		else
		{
			// TODO : 마지막 웨이포인트에 도착 -> 넥서스 근처 로직
		}
	}
	// 현재 목표 중간점으로 이동
	shared_ptr<Minion> minionSelf = static_pointer_cast<Minion>(shared_from_this());
	_room->HandleMinionMove(minionSelf, nowWp, _moveSpeed, deltaTime, _laneId);
}

void Minion::UpdateChaseTarget(float deltaTime)
{
	shared_ptr<Object> currentTarget = _currentTarget.lock();
	if (currentTarget == nullptr || currentTarget->IsDead())
	{
		currentTarget = nullptr;
		_minionState = Protocol::MinionState::MINION_LINE_TRACE;
		return;
	}

	GameMath::Vector3 minionSelfPos = GetPosVector();
	GameMath::Vector3 targetPos = currentTarget->GetPosVector();

	float distToTarget = GameMath::Vector3::GetDistTanceXZ(minionSelfPos, targetPos);

	// 공격 사거리 안이면 attack 상태 진입
	if (distToTarget <= _attackRange)
	{
		_minionState = Protocol::MinionState::MINION_ATTACK;
		return;
	}

	// 공격 사거리 밖이면 타겟 쪽으로 이동
	shared_ptr<Minion> minionSelf = static_pointer_cast<Minion>(shared_from_this());
	_room->HandleMinionMove(minionSelf, targetPos, _moveSpeed, deltaTime, _laneId);
}

void Minion::UpdateAttack(float deltaTime)
{
	shared_ptr<Object> currentTarget = _currentTarget.lock();
	if (currentTarget == nullptr || currentTarget->IsDead())
	{
		currentTarget = nullptr;
		_minionState = Protocol::MinionState::MINION_LINE_TRACE;
		return;
	}

	GameMath::Vector3 minionSelfPos = GetPosVector();
	GameMath::Vector3 targetPos = currentTarget->GetPosVector();

	float distToTarget = GameMath::Vector3::GetDistTanceXZ(minionSelfPos, targetPos);

	// 1. 타겟이 사거리 밖으로 나가면 다시 chase로 전환
	if (distToTarget > _attackRange)
	{
		_minionState = Protocol::MinionState::MINION_CHASE_TARGET;
		return;
	}
	// 2. 사거리 안 -> 쿨타임 체크 후 공격
	_attackCooldown -= deltaTime;
	if (_attackCooldown > 0.0f)
		return;

	_attackCooldown = _attackInterval;
	_room->HandleMinionAttack(currentTarget);
}

weak_ptr<Object> Minion::FindBestTarget()
{
	vector<shared_ptr<Object>> targets;
	auto minionSelf = shared_from_this();
	_room->CollectEnemiesInRange(minionSelf, _detectionRange, OUT targets);

	weak_ptr<Object> bestTarget;
	int bestPriority = INT_MAX;
	float bestDist = FLT_MAX;

	GameMath::Vector3 myPos = GetPosVector();

	for (auto target : targets)
	{
		if (target->IsDead())
			continue;

		int32 pri = GetTargetPriority(target);
		if (pri < 0)
			continue;

		GameMath::Vector3 targetVector = target->GetPosVector();
		float dist = GameMath::Vector3::GetDistTanceXZ(myPos, targetVector);

		if (pri < bestPriority || (pri == bestPriority && dist < bestDist))
		{
			bestPriority = pri;
			bestDist = dist;
			bestTarget = target;
		}
	}

	return bestTarget;
}

int32 Minion::GetTargetPriority(shared_ptr<Object> obj)
{
	// TODO : 아군 공격 금지
	if (obj->GetTeamFlag() == GetTeamFlag())
		return -1;

	Protocol::ObjectType type = obj->GetObjectType();
	if (type == Protocol::ObjectType::OBJECT_TYPE_MINION)
	{
		return 1;
	}
	else if (type == Protocol::ObjectType::OBJECT_TYPE_TURRET)
	{
		return 2;
	}
	else if (type == Protocol::ObjectType::OBJECT_TYPE_PLAYER)
	{
		return 3;
	}
	else if (type == Protocol::ObjectType::OBJECT_TYPE_NEXUS)
	{
		return 4;
	}
	else
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion] GetTargetPriority, Target Type is invalid\n");
		return 0;
	}
}

bool Minion::ShouldChaseTargetNow(shared_ptr<Object> target)
{
	GameMath::Vector3 minionSelfPos = GetPosVector();
	GameMath::Vector3 targetPos = target->GetPosVector();

	// 현재 웨이포인트
	shared_ptr<Navigation::LaneRoute> route = _route.lock();
	if (route == nullptr)
	{
		// 루트 소멸 혹은 아직 미주입
		_minionState = Protocol::MinionState::MINION_IDLE;
		return false;
	}

	// waypoint 유효성 검사
	if (route->waypoints.empty())
	{
		_minionState = Protocol::MinionState::MINION_IDLE;
		return false;
	}

	// 현재 waypoint 좌표
	GameMath::Vector3 waypoints = route->waypoints[_currentWaypointIndex];

	float distToTarget = GameMath::Vector3::GetDistTanceXZ(minionSelfPos, targetPos);
	float distToWaypoint = GameMath::Vector3::GetDistTanceXZ(minionSelfPos, route->waypoints[_currentWaypointIndex]);

	if (target->IsPlayer())
	{
		uint8 targetLane = GetLaneIdFromPos(targetPos);
		if (targetLane == _laneId)
		{
			if (distToWaypoint < distToTarget)
			{
				// 아직은 라인 우선
				return false;
			}
		}
	}
	return true;
}

uint8 Minion::GetLaneIdFromPos(GameMath::Vector3& targetPos)
{
	return 0;
}
