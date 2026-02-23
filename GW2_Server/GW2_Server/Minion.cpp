#include "pch.h"
#include "Minion.h"
#include "Room.h"
#include "NavigationSystem.h"
#include "Lobby.h"

Minion::Minion()
{
	_minionState = Protocol::MinionState::MINION_IDLE;
	_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
	_objectType = Protocol::ObjectType::OBJECT_TYPE_MINION;
}

Minion::~Minion()
{
}

void Minion::InitMinion()
{
	weak_ptr<Room> room = GLobby->GetRoomById(_roomId);
	_room = room.lock();
}

void Minion::SetMinionTarget(vector<weak_ptr<Object>>& targets)
{
	_targets.clear();
	_targets.reserve(targets.size());
	for (int32 i = 0; i < targets.size(); ++i)
	{
		_targets.emplace_back(targets[i]);
	}
}

weak_ptr<Object> Minion::FindBestTarget(vector<weak_ptr<Object>> targets)
{
	// 일부러 race condition 방지를 위해 복사해서 사용
		// Set 한 김에 best target pointer 까지 만들어주자
	shared_ptr<Object> best;
	int bestScore = INT_MIN;

	for (weak_ptr<Object> obj : targets)
	{
		shared_ptr<Object> stableObj = obj.lock();
		if (stableObj == nullptr)
			continue;

		int score = GetTargetPriority(stableObj);
		if (score > bestScore)
		{
			bestScore = score;
			best = stableObj;
		}
	}

	if (!best)
	{
		_currentTarget.reset();
		return {};
	}

	// 5) 자기 상태에 기록하고 약하게 리턴
	_currentTarget = best;
	return _currentTarget;
}

void Minion::UpdateController(float deltaTime)
{
	switch (_minionState)
	{
	case Protocol::MinionState::MINION_IDLE:
		//cout << "MinionState::IDLE" << endl;
		UpdateIdle(deltaTime);
		break;
	case Protocol::MinionState::MINION_LINE_TRACE:
		//cout << "MinionState::LINE_TRACE" << endl;
		UpdateLaneTrace(deltaTime);
		break;
	case Protocol::MinionState::MINION_CHASE_TARGET:
		cout << "MinionState::MINION_CHASE_TARGET" << endl;
		UpdateChaseTarget(deltaTime);
		break;
	case Protocol::MinionState::MINION_ATTACK:
		cout << "MinionState::MINION_ATTACK" << endl;
		UpdateAttack(deltaTime);
		break;
	case Protocol::MinionState::MINION_DEAD:
		// TODO : 미니언 제거 후 보상 지급
		break;
	}
}

void Minion::UpdateMovement(float deltaTime)
{
}

void Minion::UpdateIdle(float deltaTime)
{
	if (IsDead())
	{
		_minionState = Protocol::MinionState::MINION_DEAD;
		return;
	}

	// 공격할 타겟 있는지 확인
	shared_ptr<Object> target = FindBestTarget(_targets).lock();
	//if (target == nullptr)
		//cout << "target is nullptr" << endl;
	shared_ptr<Object> currentTarget = _currentTarget.lock();
	if (target != nullptr)
	{
		cout << "IDLE to MINION_CHASE_TARGET" << endl;
		currentTarget = target;
		_minionState = Protocol::MinionState::MINION_CHASE_TARGET;
		return;
	}

	// 아직 안갔던 waypoint가 남아있으면 이동 시작
	shared_ptr<Navigation::LaneRoute> route = _route.lock();
	if (route == nullptr)
		cout << "route is nullptr" << endl;
	if (route != nullptr)
	{
		cout << "IDLE TO MINION_LINE_TRACE" << endl;
		_minionState = Protocol::MinionState::MINION_LINE_TRACE;
		return;
	}
	// 이동 포인트에 종점이 있으면 state 종료
}

void Minion::UpdateLaneTrace(float deltaTime)
{
	GameMath::Vector3 myPos = GetPosVector();

	// repath 쿨다운 감소
	_repathCoolDown -= deltaTime;

	// 타겟 탐색
	shared_ptr<Object> target = FindBestTarget(_targets).lock();
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

	if(_currentWaypointIndex < 0 || _currentWaypointIndex >= static_cast<int32>(route->waypoints.size()))
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

			// 다음 waypoint로 넘어갈 때는 기존 path를 버리고, 다음 tick에 새 path 요청
			_path.clear();
			_pathIndex = 0;
			_lastMoveGoal = GameMath::Vector3(FLT_MAX, 0.0f, FLT_MAX);
			_repathCoolDown = 0.0f;
		}
		else
		{
			// TODO : 마지막 웨이포인트에 도착 -> 넥서스 근처 로직
			_minionState = Protocol::MinionState::MINION_IDLE;
			// TODO : StopMovement 구현
			return;
		}
	}
	// 현재 목표 중간점으로 이동
	// 매틱 마다 HandleMinionMove를 호출하지 않도록 한다
	// 조건: (1) 현재 path가 비었거나 (2) 목표가 바뀌었거나 (3) 일정 시간 지나서 재탐색 필요할 때만 요청
	auto shouldRequest = _path.empty() || (_lastMoveGoal - nowWp).Length() > 0.05f || (_repathCoolDown <= 0.0f);
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::UpdateLaneTrace] room is nullptr\n");
		return;
	}

	if (shouldRequest)
	{
		shared_ptr<Minion> minionSelf = dynamic_pointer_cast<Minion>(shared_from_this());
		cout << "should request block" << endl;
		if (minionSelf == nullptr)
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::LineTrace] minionSelf is nullptr\n");
			return;
		}
		//room->HandleMinionMove(minionSelf, nowWp, _moveSpeed, deltaTime, _laneId);
		room->DoAsync(&Room::HandleMinionMove, minionSelf, nowWp, _moveSpeed, deltaTime, _laneId);
		cout << "After Do Async" << endl;
		_lastMoveGoal = nowWp;
		_repathCoolDown = 0.2f;
	}
}

void Minion::UpdateChaseTarget(float deltaTime)
{
	shared_ptr<Object> currentTarget = _currentTarget.lock();
	if (currentTarget == nullptr || currentTarget->IsDead())
	{
		//currentTarget = nullptr;
		_minionState = Protocol::MinionState::MINION_LINE_TRACE;
		return;
	}

	_repathCoolDown -= deltaTime;

	GameMath::Vector3 minionSelfPos = GetPosVector();
	GameMath::Vector3 targetPos = currentTarget->GetPosVector();

	float distToTarget = GameMath::Vector3::GetDistTanceXZ(minionSelfPos, targetPos);

	// 공격 사거리 안이면 attack 상태 진입
	if (distToTarget <= _attackRange)
	{
		_minionState = Protocol::MinionState::MINION_ATTACK;
		return;
	}

	// 현재 목표 중간점으로 이동
	// 매틱 마다 HandleMinionMove를 호출하지 않도록 한다
	// 조건: (1) 현재 path가 비었거나 (2) 목표가 바뀌었거나 (3) 일정 시간 지나서 재탐색 필요할 때만 요청
	auto shouldRequest = _path.empty() || (_lastMoveGoal - minionSelfPos).Length() > 0.05f || (_repathCoolDown <= 0.0f);
	shared_ptr<Room> room = _room.lock();
	if (shouldRequest)
	{
		shared_ptr<Minion> minionSelf = dynamic_pointer_cast<Minion>(shared_from_this());
		//room->HandleMinionMove(minionSelf, targetPos, _moveSpeed, deltaTime, _laneId);

		_lastMoveGoal = minionSelfPos;
		_repathCoolDown = 0.2f;
	}
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
	shared_ptr<Room> room = _room.lock();
	//room->HandleMinionAttack(currentTarget);
}

bool Minion::RequestFindTarget()
{
	//vector<shared_ptr<Object>>& targets;
	shared_ptr<Minion> minionSelf = make_shared<Minion>();
	// 인자 타입 맞춰주기 위한 문장
	shared_ptr<Object> tMinionSelf = static_pointer_cast<Object>(minionSelf);
	shared_ptr<Room> room = _room.lock();

	if (tMinionSelf == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::FindBestTarget] minion self is nullptr\n");
		return false;
	}

	if (room == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::FindBestTarget] room is nullptr\n");
		return false;
	}

	room->DoAsync(&Room::CollectEnemiesInRange, tMinionSelf, _detectionRange);
	return true;
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
	// TODO : Only for test
	return 1;
}

void Minion::SetLaneRoute(shared_ptr<Navigation::LaneRoute> route)
{
	_route = route;
}

weak_ptr<Navigation::LaneRoute> Minion::GetLaneRoute() const
{
	return _route;
}
