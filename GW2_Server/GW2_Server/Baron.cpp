#include "pch.h"
#include "Baron.h"
#include "Lobby.h"
#include "Room.h"
#include "NavigationSystem.h"
#include "StatLoader.h"

const vector<int32> Baron::REWARD_CARDS = { 105, 111, 118, 119, 129 };
// Cannon, Grenade, Lava, MissileBomb, WindBlade

Baron::Baron()
{
	_baronState = Protocol::BaronState::BARON_IDLE;
	//_objectType = Protocol::ObjectType::OBJECT_TYPE_BARON;
	SetObjectType(Protocol::ObjectType::OBJECT_TYPE_BARON);
	//_campType = Protocol::CampType::CAMP_NEUTURAL;
	SetCampType(Protocol::CampType::CAMP_NEUTURAL);
	_lastMoveGoal = GameMath::Vector3(FLT_MAX, 0.f, FLT_MIN);
}

Baron::~Baron()
{
}

void Baron::InitBaron(shared_ptr<Room> room, GameMath::Vector3 spawnPos)
{
	_room = room;
	_spawnPos = spawnPos;
	_destPos = spawnPos;

	Protocol::PosInfo* tPos = _objectInfo.mutable_pos_info();
	tPos->set_x(spawnPos._x);
	tPos->set_y(spawnPos._y);
	tPos->set_z(spawnPos._z);

	UnitStat stat = GLobby->GetUnitStat("baron");
	Protocol::StatInfo* tStat = _objectInfo.mutable_stat_info();
	tStat->set_hp(stat.hp);
	tStat->set_max_hp(stat.maxHp);
	tStat->set_attack(stat.attackDamage);
	_attackRange = stat.attackRange;
	_attackInterval = stat.attackInterval;
	_moveSpeed = stat.moveSpeed;
	_detectionRange = stat.detectionRange;
	_spawnPos = GetPosVector();
}

void Baron::SetLaneRoute(shared_ptr<Navigation::LaneRoute> route)
{
	_route = route;
	// 스폰 위치는 유일한 wp
	if (route && !route->waypoints.empty())
	{
		_currentWaypointIndex = static_cast<int32>(route->waypoints.size()) / 2;
	}
}

void Baron::OnHit(int32 attackerId)
{
	// 현재 시각을 기록 (더 최근 = 더 큰 값 = SelectTarget에서 우선 선택)
		// 또는 단순히 카운트 방식으로 ++
	_aggroTable[attackerId] += 1.0f;  // 피격 횟수 누적 방식

	if (_baronState == Protocol::BaronState::BARON_IDLE)
	{
		shared_ptr<Object> target = SelectTarget();
		if (target != nullptr)
		{
			_currentTarget = target;
			_baronState = Protocol::BaronState::BARON_COMBAT;
			_aggroElapsed = 0.0f;
		}
	}
}

void Baron::UpdateController(float deltaTime)
{
	if (_stunTimer > 0.f)
	{
		_stunTimer -= deltaTime;
		return;
	}

	switch (_baronState)
	{
	case Protocol::BaronState::BARON_IDLE:   UpdateIdle(deltaTime);   break;
	//case Protocol::BaronState::BARON_PATROL: UpdatePatrol(deltaTime); break;
	case Protocol::BaronState::BARON_COMBAT: UpdateCombat(deltaTime); break;
	case Protocol::BaronState::BARON_DEAD:   break;
	}
}

void Baron::UpdateMovement(float deltaTime)
{
	Protocol::PosInfo* tPos = _objectInfo.mutable_pos_info();
	
	if (tPos->state() != Protocol::MoveState::MOVE_STATE_RUN)
	{
		_movement.speed = 0.f;
		return;
	}

	if (_pathIndex >= static_cast<int32>(_path.size()))
	{
		tPos->set_state(Protocol::MoveState::MOVE_STATE_RUN);
		_isMoving = false;
		_movement.speed = 0.0f;
		return;
	}

	const float eps = 1e-4f;
	float remain = _moveSpeed * deltaTime;
	while (remain > 0.0f && _pathIndex < static_cast<int32>(_path.size()))
	{
		const GameMath::Vector3& tgt = _path[_pathIndex];
		GameMath::Vector3 dir = tgt - _posVector;
		float dist = dir.Length();
		if (dist <= eps)
		{
			_posVector = tgt;
			++_pathIndex;
			continue;
		}

		if (dist <= remain)
		{
			_posVector = tgt;
			remain -= dist;
			++_pathIndex;
			continue;
		}

		dir = dir.Normalized();
		_posVector = _posVector + dir * remain;
		tPos->set_x(_posVector._x);
		tPos->set_y(_posVector._y);
		tPos->set_z(_posVector._z);
		SetPosInfo(*tPos);
		_isMoving = true;
		_movement.speed = _moveSpeed;
		
		return;
	}

	tPos->set_x(_posVector._x);
	tPos->set_y(_posVector._y);
	tPos->set_z(_posVector._z);
	SetPosInfo(*tPos);
	if (_pathIndex >= static_cast<int32>(_path.size()))
	{
		//_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
		tPos->set_state(Protocol::MoveState::MOVE_STATE_IDLE);
		_isMoving = false;
		_movement.speed = 0.0f;
	}
}

void Baron::RequestMove(vector<GameMath::Vector3> path)
{
	_path = path;
	_pathIndex = 0;
	//_moveState = Protocol::MoveState::MOVE_STATE_RUN;
	Protocol::PosInfo* tPos = _objectInfo.mutable_pos_info();
	tPos->set_state(Protocol::MoveState::MOVE_STATE_RUN);
}


void Baron::UpdateIdle(float deltaTime)
{
	shared_ptr<Object> target = SelectTarget();
	if (target != nullptr)
	{
		_currentTarget = target;
		_baronState = Protocol::BaronState::BARON_COMBAT;
		_aggroElapsed = 0.0f;
	}
}

//void Baron::UpdatePatrol(float deltaTime)
//{
//	// 주변 탐색
//	shared_ptr<Room> room = _room.lock();
//	if (room == nullptr)
//		return;
//
//	if (SelectTarget() != nullptr)
//	{
//		_baronState = Protocol::BaronState::BARON_COMBAT;
//		return;
//	}
//
//	// 웨이 포인트 순찰
//}

void Baron::UpdateCombat(float deltaTime)
{
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr) return;

	// _currentTarget 유효성 확인 후 재탐색
	shared_ptr<Object> target = _currentTarget.lock();
	if (target == nullptr || target->IsDead())
	{
		target = SelectTarget();
		if (target == nullptr)
		{
			_aggroElapsed += deltaTime;
			if (_aggroElapsed >= NO_TARGET_RESET) ResetBaron();
			return;
		}
		_currentTarget = target;
	}
	_aggroElapsed = 0.0f;

	// 스킬 타이머 감소 -> 타겟이 있을 때만
	_skill1Timer -= deltaTime;
	_skill2Timer -= deltaTime;

	// 리시 체크 : 스폰 포인트로부터 너무 멀어지면 리셋 된다.
	GameMath::Vector3 nowPos = GetPosVector();
	float distFromSpawn = GameMath::Vector3::GetDistTanceXZ(nowPos, _spawnPos);
	if (distFromSpawn > _leashRange)
	{
		ResetBaron();
		return;
	}

	GameMath::Vector3 targetPos = target->GetPosVector();
	float distToTarget = GameMath::Vector3::GetDistTanceXZ(nowPos, targetPos);

	// 공격 범위 밖 -> 추격
	if (distToTarget > _attackRange)
	{
		_repathCoolDown -= deltaTime;
		bool goalChanged = (_lastMoveGoal - targetPos).Length() > 0.5f;
		bool shouldRequest = !_pathPending && (_path.empty() || goalChanged) && _repathCoolDown <= 0.0f;
		if (shouldRequest)
		{
			_pathPending = true;
			shared_ptr<Baron> self = dynamic_pointer_cast<Baron>(shared_from_this());
			room->DoAsync(&Room::HandleBaronChase, self, targetPos, _moveSpeed, deltaTime);
			_lastMoveGoal = targetPos;
			_repathCoolDown = 0.2f;
		}
		return;
	}

	// 공격 범위 내
	_path.clear();
	_pathIndex = 0;
	Protocol::PosInfo* tPos = _objectInfo.mutable_pos_info();
	//_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
	tPos->set_state(Protocol::MoveState::MOVE_STATE_IDLE);

	// 스킬2 : 독장판, 일시뎀
	if (_skill2Timer <= 0.0f)
	{
		shared_ptr<Baron> self = dynamic_pointer_cast<Baron>(shared_from_this());
		room->DoAsync(&Room::HandleBaronAoe, self, 2);
		_skill2Timer = SKILL2_INTERVAL;
		return;
	}

	// 스킬1 : AOE 슬램
	if (_skill1Timer <= 0.0f)
	{
		shared_ptr<Baron> self = dynamic_pointer_cast<Baron>(shared_from_this());
		room->DoAsync(&Room::HandleBaronAoe, self, 1);
		_skill1Timer = SKILL1_INTERVAL;
		return;
	}

	// 평타
	_attackCooldown -= deltaTime;
	if (_attackCooldown <= 0.0f)
	{
		shared_ptr<Baron> self = dynamic_pointer_cast<Baron>(shared_from_this());
		room->DoAsync(&Room::HandleBaronAttack, self, target->GetObjectId());
		_attackCooldown = _attackInterval;
	}
}

shared_ptr<Object> Baron::SelectTarget()
{
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
		return nullptr;

	auto objects = room->GetRoomObjects();

	// aggro 테이블에서 가장 최근 피격 생존 플레이어
	int32 bestId = -1;
	float bestTime = -1.f;
	for (auto& [id, t] : _aggroTable)
	{
		auto it = objects.find(id);
		if (it == objects.end() || it->second->IsDead())
			continue;

		if (t > bestTime)
		{
			bestTime = t;
			bestId = id;
		}
	}

	if (bestId != -1)
	{
		auto it = objects.find(bestId);
		if (it != objects.end())
			return it->second;
	}

	// detection range 내 가장 가까운 플레이어
	GameMath::Vector3 myPos = GetPosVector();
	shared_ptr<Object> nearest;
	float minDist = FLT_MAX;
	for (auto& [id, obj] : objects)
	{
		if (obj->GetTeamFlag() == Protocol::CampType::CAMP_NEUTURAL)
			continue;

		if (obj->IsDead() || obj->IsPlayer() == false)
			continue;

		GameMath::Vector3 targetVec = obj->GetPosVector();
		float d = GameMath::Vector3::GetDistTanceXZ(myPos, targetVec);
		if (d < _detectionRange && d < minDist)
		{
			minDist = d;
			nearest = obj;
		}
	}
	return nearest;
}

bool Baron::HasLivingAggroTarget()
{
	return false;
}

void Baron::ResetBaron()
{
	_aggroTable.clear();
	_aggroElapsed = 0.0f;
	_skill1Timer = SKILL1_INTERVAL;
	_skill2Timer = SKILL2_INTERVAL;
	_attackCooldown = 0.0f;
	_baronState = Protocol::BaronState::BARON_IDLE;
	_objectInfo.mutable_stat_info()->set_hp(_objectInfo.stat_info().max_hp());
	//_objectInfo.set_hp
	_path.clear();
	_pathIndex = 0;
	_isMoving = false;
	//_moveState = Protocol::MoveState::MOVE_STATE_IDLE;
	SetMoveState(Protocol::MoveState::MOVE_STATE_IDLE);
	_repathCoolDown = 0.0f;
	_lastMoveGoal = GameMath::Vector3(FLT_MAX, 0.0f, FLT_MAX);
	SetPosVector(_spawnPos);

	Protocol::PosInfo* tPos = _objectInfo.mutable_pos_info();
	tPos->set_x(_spawnPos._x);
	tPos->set_y(_spawnPos._y);
	tPos->set_z(_spawnPos._z);
	SetPosInfo(*tPos);
}

void Baron::OnDead()
{
	_baronState = Protocol::BaronState::BARON_DEAD;
}