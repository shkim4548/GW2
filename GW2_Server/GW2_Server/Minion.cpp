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

	_currentWaypointIndex = 0;
	_repathCoolDown = 0.0f;
	_lastMoveGoal = GameMath::Vector3(FLT_MAX, 0.0f, FLT_MAX);

	// Stat 초기화
	_statInfo.set_hp(100);
	_statInfo.set_max_hp(100);
	_statInfo.set_attack(10);
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
	// �Ϻη� race condition ������ ���� �����ؼ� ���
		// Set �� �迡 best target pointer ���� ���������
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

	// 5) �ڱ� ���¿� ����ϰ� ���ϰ� ����
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
		//cout << "MinionState::MINION_CHASE_TARGET" << endl;
		UpdateChaseTarget(deltaTime);
		break;
	case Protocol::MinionState::MINION_ATTACK:
		//cout << "MinionState::MINION_ATTACK" << endl;
		UpdateAttack(deltaTime);
		break;
	case Protocol::MinionState::MINION_DEAD:
		// TODO : �̴Ͼ� ���� �� ���� ����
		break;
	}
}

void Minion::UpdateMovement(float deltaTime)
{
	if (_moveState != Protocol::MoveState::MOVE_STATE_RUN)
	{
		_movement.speed = 0.0f;
		return;
	}

	if (_pathIndex >= static_cast<int32>(_path.size()))
	{
		_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
		_isMoving = false;
		_movement.speed = 0.0f;
		return;
	}

	const float kArriveEpsilon = 1e-4f;
	float remainMoveDist = _moveSpeed * deltaTime;

	while (remainMoveDist > 0.0f && _pathIndex < static_cast<int32>(_path.size()))
	{
		const GameMath::Vector3& target = _path[_pathIndex];
		GameMath::Vector3 dir = target - _posVector;
		const float dist = dir.Length();

		// Ÿ�� ��忡 ����� ����
		if (dist <= kArriveEpsilon)
		{
			_posVector = target;
			++_pathIndex;
			continue;
		}

		// �̹� �����ӿ� target���� ���� ���� -> target���� �̵��ϰ� ���� ����
		if (dist <= remainMoveDist)
		{
			_posVector = target;
			remainMoveDist -= dist;
			++_pathIndex;
			continue;
		}

		// target������ �� ���Ƿ�, �� �������� remainMoveDist ��ŭ�� �̵��Ѵ�
		dir = dir.Normalized();
		GameMath::Vector3 delta = dir * remainMoveDist;
		_posVector = _posVector + delta;

		// PosInfo ����ȭ
		_pos.set_x(_posVector._x);
		_pos.set_y(_posVector._y);
		_pos.set_z(_posVector._z);
		SetPosInfo(_pos);
		// �̵��� ���� ����
		_isMoving = true;
		_movement.speed = _moveSpeed;
		// �̵� �� (remainMoveDist ���� �� return ����)
		//GConsoleLogger->WriteStdOut(Color::GREEN,
		//	L"[Minion::UpdateMovement] moving. posVector=(%.3f, %.3f) pathIndex=%d\n",
		//	_posVector._x, _posVector._z, _pathIndex);
		return;
	}
	//GConsoleLogger->WriteStdOut(Color::GREEN,
	//	L"[Minion::UpdateMovement] path done. posVector=(%.3f, %.3f) pathIndex=%d pathSize=%d\n",
	//	_posVector._x, _posVector._z,
	//	_pathIndex, static_cast<int32>(_path.size()));

	// path�� �� �Һ��� ���
	_pos.set_x(_posVector._x);
	_pos.set_y(_posVector._y);
	_pos.set_z(_posVector._z);
	SetPosInfo(_pos);

	if (_pathIndex >= static_cast<int32>(_path.size()))
	{
		_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
		_isMoving = false;
		_movement.speed = 0.0f;
		_repathCoolDown = 0.0f;
	}
}

void Minion::UpdateIdle(float deltaTime)
{
	if (IsDead())
	{
		_minionState = Protocol::MinionState::MINION_DEAD;
		return;
	}

	// ������ Ÿ�� �ִ��� Ȯ��
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

	// 남은 waypoint가 있을 때만 LINE_TRACE로 전환, 모두 완료했으면 IDLE 유지
	shared_ptr<Navigation::LaneRoute> route = _route;
	if (route != nullptr)
	{
		if (_currentWaypointIndex < static_cast<int32>(route->waypoints.size()))
		{
			//GConsoleLogger->WriteStdOut(Color::WHITE, L"IDLE TO MINION_LINE_TRACE\n");
			_minionState = Protocol::MinionState::MINION_LINE_TRACE;
			return;
		}
		// _currentWaypointIndex >= size → 모든 waypoint 완료 → IDLE 유지
	}
}

void Minion::UpdateLaneTrace(float deltaTime)
{
	GameMath::Vector3 myPos = GetPosVector();
	//GConsoleLogger->WriteStdOut(Color::WHITE,
	//	L"[LaneTrace] myPos=(%.3f,%.3f) wpIdx=%d pathSize=%d moveState=%d repathCD=%.3f\n",
	//	myPos._x, myPos._z,
	//	_currentWaypointIndex,              // �� wpIdx
	//	static_cast<int32>(_path.size()),   // �� pathSize
	//	static_cast<int32>(_moveState),     // �� moveState
	//	_repathCoolDown);
	_repathCoolDown -= deltaTime;
	_findTargetCoolDown -= deltaTime;

	// 1. 주기적으로 주변 적 탐색 요청
	if (_findTargetCoolDown <= 0.0f)
	{
		RequestFindTarget();
		_findTargetCoolDown = 0.5f;
	}

	// 2) Chase ��ȯ üũ
	shared_ptr<Object> target = FindBestTarget(_targets).lock();
	if (target != nullptr)
	{
		if (ShouldChaseTargetNow(target))
		{
			_currentTarget = target;
			_minionState = Protocol::MinionState::MINION_CHASE_TARGET;
			return;
		}
	}

	// 2) LaneRoute ��ȿ�� üũ
	shared_ptr<Navigation::LaneRoute> route = _route;
	if (route == nullptr || route->waypoints.empty())
	{
		_minionState = Protocol::MinionState::MINION_IDLE;
		return;
	}

	if (_currentWaypointIndex < 0 ||
		_currentWaypointIndex >= static_cast<int32>(route->waypoints.size()))
	{
		_currentWaypointIndex = 0;
	}

	GameMath::Vector3 nowWp = route->waypoints[_currentWaypointIndex];
	float distToNextPoint = GameMath::Vector3::GetDistTanceXZ(myPos, nowWp);

	// 3) waypoint ��ó�̸� ���� + ���� waypoint ��ȯ
	//const float kSnapDistance = _moveSpeed * 0.1f * 2.0f;
	const float kSnapDistance = 1.0f;

	if (distToNextPoint <= kSnapDistance)
	{
		// ���� ��ǥ�� ��Ȯ�� wp�� ����� (posVector + PosInfo ����ȭ)
		SetPosVector(nowWp);  // _posVector, _pos �� �� ���õǵ��� ����

		// �� �̻� �� wp�� ���� path�� �ʿ� ����
		_path.clear();
		_pathIndex = 0;
		_isMoving = false;
		_movement.speed = 0.0f;

		MarkForceBroadcastMove();
		_moveState = Protocol::MoveState::MOVE_STATE_IDLE;

		if (_currentWaypointIndex + 1 < static_cast<int32>(route->waypoints.size()))
		{
			++_currentWaypointIndex;
			_lastMoveGoal = GameMath::Vector3(FLT_MAX, 0.0f, FLT_MAX);
			_repathCoolDown = 0.0f;
			return;
		}
		else
		{
			_minionState = Protocol::MinionState::MINION_IDLE;
			return;
		}
	}
	// 4) Path 요청 판단
	// repathCoolDown 음수 누적 방지
	if (_repathCoolDown < 0.0f)
		_repathCoolDown = 0.0f;

	bool goalChanged = (_lastMoveGoal - nowWp).Length() > 0.05f;
	bool isMovingToSameGoal = (_isMoving || _pathPending) && !_path.empty() && !goalChanged;
	bool shouldRequest = !isMovingToSameGoal
		&& !_pathPending                      // DoAsync 실행 중이면 재요청 차단
		&& (_path.empty() || goalChanged)
		&& (_repathCoolDown <= 0.0f);
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::UpdateLaneTrace] room is nullptr\n");
		return;
	}

	if (shouldRequest)
	{
		shared_ptr<Minion> minionSelf = dynamic_pointer_cast<Minion>(shared_from_this());
		if (minionSelf == nullptr)
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::UpdateLaneTrace] minionSelf is nullptr\n");
			return;
		}

		_pathPending = true;
		int32 capturedWpIndex = _currentWaypointIndex;
		room->DoAsync(&Room::HandleMinionMove, minionSelf, nowWp, _moveSpeed, deltaTime, _laneId, capturedWpIndex);
		_lastMoveGoal = nowWp;
		_repathCoolDown = 0.5f;
	}
}

void Minion::UpdateChaseTarget(float deltaTime)
{
	shared_ptr<Object> currentTarget = _currentTarget.lock();
	if (currentTarget == nullptr || currentTarget->IsDead())
	{
		_minionState = Protocol::MinionState::MINION_LINE_TRACE;
		return;
	}

	_repathCoolDown -= deltaTime;

	GameMath::Vector3 minionSelfPos = GetPosVector();
	GameMath::Vector3 targetPos = currentTarget->GetPosVector();
	float distToTarget = GameMath::Vector3::GetDistTanceXZ(minionSelfPos, targetPos);

	// 공격 범위 진입 -> Attack 상태로 전환
	if (distToTarget <= _attackRange)
	{
		_minionState = Protocol::MinionState::MINION_ATTACK;
		return;
	}

	// leash이탈 -> LINETRACE 복귀
	if (distToTarget > _detectionRange * 1.5f)
	{
		_currentTarget.reset();
		_minionState = Protocol::MinionState::MINION_LINE_TRACE;
		return;
	}

	// 경로 요청 판단
	bool goalChanged = (_lastMoveGoal - targetPos).Length() > 0.5f;
	bool shouldRequest = (_path.empty() || goalChanged) && (_repathCoolDown <= 0.0f);

	if (shouldRequest == true)
	{
		shared_ptr<Room> room = _room.lock();
		if (room == nullptr)
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::UpdateChaseTarget] room is nullptr\n");
			return;
		}

		shared_ptr<Minion> minionSelf = dynamic_pointer_cast<Minion>(shared_from_this());
		if (minionSelf == nullptr)
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::UpdateChaseTarget] minionSelf is nullptr\n");
			return;
		}
		room->DoAsync(&Room::HandleChaseMove, minionSelf, targetPos, _moveSpeed, deltaTime, _laneId);
		_lastMoveGoal = targetPos;
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

	// 1. Ÿ���� ��Ÿ� ������ ������ �ٽ� chase�� ��ȯ
	if (distToTarget > _attackRange)
	{
		_minionState = Protocol::MinionState::MINION_CHASE_TARGET;
		return;
	}
	// 2. ��Ÿ� �� -> ��Ÿ�� üũ �� ����
	_attackCooldown -= deltaTime;
	if (_attackCooldown > 0.0f)
		return;

	_attackCooldown = _attackInterval;
	shared_ptr<Room> room = _room.lock();
	shared_ptr<Minion> self = dynamic_pointer_cast<Minion>(shared_from_this());
	//room->HandleMinionAttack(currentTarget);
	room->DoAsync(&Room::HandleMinionAttack, self, currentTarget);
}

bool Minion::RequestFindTarget()
{
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::RequestFindTarget] room is nullptr\n");
		return false;
	}

	shared_ptr<Object> minionSelf = dynamic_pointer_cast<Object>(shared_from_this());
	if (minionSelf == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::RequestFindTarget] minionSelf is nullptr\n");
		return false;
	}

	room->DoAsync(&Room::CollectEnemiesInRange, minionSelf, _detectionRange);
	return true;
}

int32 Minion::GetTargetPriority(shared_ptr<Object> obj)
{
	// TODO : �Ʊ� ���� ����
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

	// ���� ��������Ʈ
	shared_ptr<Navigation::LaneRoute> route = _route;
	if (route == nullptr)
	{
		// ��Ʈ �Ҹ� Ȥ�� ���� ������
		_minionState = Protocol::MinionState::MINION_IDLE;
		return false;
	}

	// waypoint ��ȿ�� �˻�
	if (route->waypoints.empty())
	{
		_minionState = Protocol::MinionState::MINION_IDLE;
		return false;
	}

	// ���� waypoint ��ǥ
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
				// ������ ���� �켱
				return false;
			}
		}
	}
	return true;
}

uint8 Minion::GetLaneIdFromPos(GameMath::Vector3& targetPos)
{
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::GetLaneIdFromPos] room is nullptr");
		return 0;
	}

	//Navigation::NavigationSystem navSystem = room->
	// ��� room�� �ϳ��� navmesh�� ����(�� ������ �ϳ�)
	shared_ptr<Navigation::NavigationSystem> navSystem = GLobby->GetNavigationSystem().lock();
	Navigation::WalkableGrid& grid = navSystem->GetGridCells();

	int32 gx, gz;
	if (!navSystem->WorldToGrid(grid, targetPos, gx, gz))
		return 0;

	const Navigation::GridCell& cell = grid.At(gx, gz);
	return static_cast<uint8>(cell.laneId);
}

void Minion::SetLaneRoute(shared_ptr<Navigation::LaneRoute> route)
{
	_route = route;
}

weak_ptr<Navigation::LaneRoute> Minion::GetLaneRoute() const
{
	return weak_ptr<Navigation::LaneRoute>(_route);
}

void Minion::OnDead()
{
	// TODO : 플레이어에게 보상 지급
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion::OnDead] room is nullptr\n");
		return;
	}

	room->DoAsync(&Room::HandleRemoveObject, _objectId);
}
