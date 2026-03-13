#include "pch.h"
#include "Lobby.h"
#include "Room.h"
#include "Player.h"
#include "GameSession.h"
#include "NavigationSystem.h"
#include "NavmeshLoader.h"
#include "ClientPacketHandler.h"
#include "Minion.h"
#include "ObjectUtils.h"
#include "LaneRouteLoader.h"

// 공용으로 사용할 전역 룸
//shared_ptr<Room> GRoom = make_shared<Room>();	//모든 클라를 여기에 접속시켜서 확인한다.

Room::Room()
{
	_navigationSystem = GLobby->GetNavigationSystem();
	_roomWalkableGrid = GLobby->GetWalkableGrid();
}

Room::~Room()
{
	_players.clear();

}

bool Room::Enter(PlayerRef gameObject)
{
	if (gameObject == nullptr)
	{
		return false;
	}

	//GConsoleLogger->WriteStdOut(Color::YELLOW, L"[EnterGameHandler] player Enter Game Room\n");

	int32 objectId = gameObject->GetObjectId();
	_objects.emplace(objectId, gameObject);
	GConsoleLogger->WriteStdOut(
		Color::GREEN,
		L"[Room::SpawnMinion] objId=%d pos=(%.2f,%.2f)\n",
		gameObject->GetObjectId(),
		gameObject->GetPosVector()._x,
		gameObject->GetPosVector()._z);
	Protocol::S_ENTER_GAME enterPkt;
	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();

	GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::Enter] enterPlayer\n");
	objectInfo->set_object_type(Protocol::OBJECT_TYPE_PLAYER);
	objectInfo->set_object_id(objectId);
	posInfo->set_x(72.5);
	posInfo->set_y(0);
	posInfo->set_z(0);
	posInfo->set_yaw(0);
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);
	_players.emplace(objectId, gameObject);
	
	// TODO : 나중에 시작 플래그 패킷으로 받는걸로 바꿔야함
	_isRunning = true;
	GameMath::Vector3 spawnPos(72.5f, 0.0f, 0.0f);
	gameObject->SetPosVector(spawnPos);

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(enterPkt);
	Broadcast(sendBuffer);
	return true;
}

void Room::Leave(int32 playerId)
{

}

void Room::Broadcast(SendBufferRef sendBuffer, int32 exceptId)
{
	for (auto& p : _players)
	{
		PlayerRef player = dynamic_pointer_cast<Player>(p.second);
		if (player == nullptr)
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[Room::Broadcast] player is nullptr\n");
			continue;
		}

		if (player->GetPlayerId() == exceptId)
		{
			continue;
		}

		if (GameSessionRef session = player->GetSession().lock())
		{
			//cout << "Broadcast" << '\n';
			session->Send(sendBuffer);
		}
	}
}

void Room::RoomInit(unordered_map<int32, shared_ptr<Navigation::LaneRoute>> route)
{
	_laneRoute = route;
	for (const auto& [laneId, route] : _laneRoute)
	{
		if (route == nullptr)
			continue;
		GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Room::RoomInit] laneId=%d, waypoints=%d\n",
			laneId, static_cast<int32>(route->waypoints.size()));
	}
}

bool Room::HandleEnterPlayer(PlayerRef player)
{
	return false;
}

bool Room::HandleSkill(ObjectRef attacker, Protocol::C_SKILL skillPkt)
{
	if (attacker == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleSkill] attacker is nullptr\n");
		return false;
	}

	auto targetIter = _objects.find(skillPkt.target_id());
	if (targetIter == _objects.end())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleSkill] target not found\n");
		return false;
	}

	shared_ptr<Object> target = targetIter->second;

	// 팀체크
	if (attacker->GetTeamFlag() == target->GetTeamFlag())
	{
		return false;
	}

	// 거리 체크
	// TODO : 하드코딩, 3.0f
	const float attackRange = 3.0f;
	GameMath::Vector3 attackerPos = attacker->GetPosVector();
	GameMath::Vector3 targetPos = target->GetPosVector();
	float dist = GameMath::Vector3::GetDistTanceXZ(attackerPos, targetPos);
	if (dist > attackRange)
	{
		GConsoleLogger->WriteStdErr(Color::YELLOW, L"[Room::HandleSkill] out of range dist=%.2f\n", dist);
		return false;
	}

	// SkillType 분기
	switch (skillPkt.skill_id())
	{
	case Protocol::SkillType::SKILL_ID_ATTACK:
	{
		const uint64_t damage = 10; // 임시 고정값
		bool died = target->ApplyDamage(damage);

		GConsoleLogger->WriteStdOut(Color::GREEN,
			L"[Room::HandleSkill] attacker=%lld target=%d dmg=%llu hp=%llu died=%d\n",
			attacker->GetObjectId(), target->GetObjectId(),
			damage, target->GetHp(), died ? 1 : 0);

		// TODO: died == true 시 사망 처리

		// S_SKILL 브로드캐스트
		Protocol::S_SKILL resPkt;
		resPkt.set_skill_id(skillPkt.skill_id());
		resPkt.set_attacker_id(attacker->GetObjectId());
		resPkt.set_target_id(skillPkt.target_id());

		SendBufferRef buf = ClientPacketHandler::MakeSendBuffer(resPkt);
		Broadcast(buf);
		break;
	}
	default:
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleSkill] unknown skill_id\n");
		break;
	}
	return true;
}

void Room::HandleMovePlayer(Protocol::C_MOVE movePkt)
{
	Protocol::PosInfo startPos = movePkt.start_pos();
	Protocol::PosInfo endPos = movePkt.target_pos();

	GameMath::Vector3 startWorld(startPos.x(), startPos.y(), startPos.z());
	GameMath::Vector3 endWorld(endPos.x(), endPos.y(), endPos.z());

	//cout << "[Room::HandleMovePlayer] Before FindPath" << endl;
	// World -> Grid
	int32 sx, sz, tx, tz;
	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	if (navSystem == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] _navigationSystem is nullptr\n");
		return;
	}

	Navigation::WalkableGrid& grid = _navigationSystem.lock()->GetGridCells();
	if (!_navigationSystem.lock()->WorldToGrid(grid, startWorld, sx, sz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] WorldToGrid Fail\n");
		return;
	}

	if (!_navigationSystem.lock()->WorldToGrid(grid, endWorld, tx, tz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] WorldToGrid Fail\n");
		return;
	}

	// PathFinding
	vector<Navigation::GridCell*> gridPath;
	bool ok = _navigationSystem.lock()->FindPath(grid, sx, sz, tx, tz, gridPath, 0);

	if (!ok || gridPath.empty())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] GridCell is nullptr\n");
		// TODO : 이동 실패 구현
		return;
	}

	int32 playerId = movePkt.object_id();
	weak_ptr<Player> player = _players[playerId];
	if (player.lock() == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] player is nullptr\n");
		// TODO : 이동 실패 구현
		return;
	}
	//cout << "End of HandleMovePlayer" << endl;
	HandleMovePlayerInternal(player.lock(), gridPath, startWorld, endWorld);
}

bool Room::HandleSpawnMinion(MinionRef minion)
{
	if (minion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleSpawnMinion] minion is nullptr\n");
		return false;
	}

	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();
	Protocol::S_ENTER_GAME enterPkt;

	//minion = ObjectUtils::CreateMinion();
	int32 objectId = minion->GetMinionId();

	GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::Enter] enterMinion\n");
	objectInfo->set_object_type(Protocol::OBJECT_TYPE_MINION);
	objectInfo->set_object_id(objectId);
	objectInfo->set_team_flag(Protocol::CampType::CAMP_CYBORG);
	posInfo->set_x(minion->GetPosInfo().x());
	posInfo->set_y(minion->GetPosInfo().y());
	posInfo->set_z(minion->GetPosInfo().z());
	posInfo->set_yaw(0);
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);
	_objects.emplace(objectId, minion);

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(enterPkt);
	Broadcast(sendBuffer);
	return true;
}

// Path를 player에 할당한다.
void Room::HandleMovePlayerInternal(PlayerRef player, std::vector<Navigation::GridCell*>& gridPath, const GameMath::Vector3& startWorld, const GameMath::Vector3& endWorld)
{
	vector<GameMath::Vector3> worldPath;
	worldPath.reserve(gridPath.size() + 2);
	worldPath.push_back(startWorld);

	for (Navigation::GridCell* cell : gridPath)
	{
		GameMath::Vector3 worldPos;
		if (!_navigationSystem.lock()->GridToWorld(_navigationSystem.lock()->GetGridCells(), cell->x, cell->z, worldPos))
		{
			continue;
		}

		if ((worldPos - startWorld).Length() < 0.01f)
		{
			continue;
		}

		worldPath.push_back(worldPos);
	}

	if (worldPath.empty() || (worldPath.back() - endWorld).Length() >= 0.01f)
	{
		worldPath.push_back(endWorld);
	}

	if (player == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[HandleMovePlayerInternal] player is nullptr");
		return;
	}
	// 이 부분이 빠져있었음
	//player->GetMoveState();
	player->SetMoveState(Protocol::MOVE_STATE_RUN);
	player->_path = move(worldPath);
	player->_pathIndex = 0;
	player->SetIsMoving(true);
}

void Room::UpdateRoom(float deltaTime)
{
	// 0. 미니언 스폰 (기존 로직 유지)
	_minionSpawnAccumulate += deltaTime;
	if (_isRunning)
	{
		while (_minionSpawnAccumulate >= _minionSpawnCoolDown)
		{
			_minionSpawnAccumulate -= _minionSpawnCoolDown;

			// TEMP : For TEST, left base location hard coding
			GameMath::Vector3 tempPos;
			tempPos._x = -54;
			tempPos._y = 0;
			tempPos._z = 105;
			SpawnMinion(1, Protocol::CAMP_CYBORG);
		}
	}

	// 1) Controller Phase
	for (auto& [id, obj] : _objects)
	{
		if (obj == nullptr)
			continue;

		obj->UpdateController(deltaTime);  // Player/Minion 공통
	}

	// 2) Movement + Broadcast Phase
	for (auto& [id, obj] : _objects)
	{
		if (obj == nullptr)
			continue;

		bool wasMoving = obj->GetIsMoving();

		obj->UpdateMovement(deltaTime);

		bool isMoving = obj->GetIsMoving();

		obj->AccumulateMoveTime(deltaTime);

		if (obj->ShouldBroadcastMove())
		{
			BroadcastMoving(obj);
			obj->ResetBroadcastTimer();
		}

		if (wasMoving && !isMoving)
			BroadcastMovingEnd(obj);

		obj->PostUpdate();
	}
}

shared_ptr<Minion> Room::SpawnMinion(int32 laneId, Protocol::CampType team)
{
	// route 확보
	shared_ptr<Navigation::LaneRoute> route = GetLaneRoute(laneId).lock();
	if (route == nullptr || route->waypoints.empty())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::SpawnMinion] LaneRoute missing. laneId=%d\n", laneId);
		return nullptr;
	}

	// 스폰 위치 초기화
	const GameMath::Vector3& spawnWorldPos = route->waypoints.front();

	// 미니언 생성
	shared_ptr<Minion> minion = ObjectUtils::CreateMinion();
	if (minion == nullptr)
		return nullptr;

	GConsoleLogger->WriteStdOut(
		Color::GREEN,
		L"[Room::SpawnMinion] objId=%d minionId=%d laneId=%d pos=(%.2f,%.2f)\n",
		minion->GetObjectId(),
		minion->GetMinionId(),
		minion->GetLaneId(),
		minion->GetPosVector()._x,
		minion->GetPosVector()._z);

	// 기본 파라미터 세팅
	minion->SetLaneRoute(route);
	//minion->_laneId = static_cast<uint8>(laneId);
	minion->SetMinionLaneId(laneId);

	// 위치 초기화
	Protocol::PosInfo posInfo;
	posInfo.set_x(spawnWorldPos._x);
	posInfo.set_y(spawnWorldPos._y);
	posInfo.set_z(spawnWorldPos._z);
	minion->SetPosInfo(posInfo);
	minion->SetRoomId(this->GetRoomId());
	minion->SetMinionTeam(team);
	minion->InitMinion();

	//GConsoleLogger->WriteStdOut(Color::GREEN, L"SpawnMinion\n");
	// Room에 등록한다
	HandleSpawnMinion(minion);
	return minion;
}

void Room::CollectEnemiesInRange(const shared_ptr<Object> requester, float range)
{
	vector<weak_ptr<Object>> rets;
	if (requester == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::CollectEnemiesInRange] requester is nullptr\n");
		return;
	}

	const Protocol::CampType team = requester->GetTeamFlag();
	const GameMath::Vector3 requesterPos = requester->GetPosVector();
	const float rangeSquare = range * range;
	// 선형탐색의 범위를 자신의 라인 안으로만 한정한다.
	//const shared_ptr<Minion>& asMinion = requester->IsMinion() ? static_pointer_cast<Minion>(requester) : nullptr;
	auto asMinion = dynamic_pointer_cast<Minion>(requester);
	const uint8 myLaneId = asMinion ? asMinion->_laneId : 0;
	if (asMinion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::CollectEnemiesInRange] asMinion is nullptr\n");
		return;
	}
	// 이 반복문에 들어가지도 못하고 함수 다운
	for (auto& [id, obj] : _objects)
	{
		if (obj == nullptr)
		{
			cout << "obj is nullptr" << endl;
			continue;
		}

		if (!obj || obj == requester /*|| obj->IsDead()*/)
			continue;

		if (obj->GetTeamFlag() == team)
			continue;

		// (선택) 미니언이면 같은 laneId 대상만
		if (asMinion && obj->IsMinion())
		{
			// 타겟이 플레이어/미니언/포탑일 수 있으니, laneId를 어떻게 꺼낼지 정책 필요
			// 가장 단순: 타겟 위치로 grid에서 laneId 조회
			GameMath::Vector3 nowPos = obj->GetPosVector();
			uint8 targetLaneId = _navigationSystem.lock()->GetLaneId(_navigationSystem.lock()->GetGridCells(), nowPos);
			if (targetLaneId != myLaneId)
				continue;
		}

		const GameMath::Vector3 p = obj->GetPosVector();
		float dx = p._x - requesterPos._x;
		float dz = p._z - requesterPos._z;
		if (dx * dx + dz * dz <= rangeSquare)
			rets.push_back(obj);
	}
	asMinion->SetMinionTarget(rets);
}

void Room::HandleMinionMove(shared_ptr<Minion> minion, GameMath::Vector3 dest, float speed, float deltaTime, uint8 laneId, int32 wpIndex)
{
	// _pathPending은 이 함수가 종료될 때 반드시 해제되어야 함 (성공/실패 무관)
	struct PendingGuard {
		shared_ptr<Minion>& m;
		~PendingGuard() {
			if (m) m->ClearPathPending();
		}
	} guard{ minion };

	if (minion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] minion is nullptr\n");
		return;
	}
	// DEBUG
	GameMath::Vector3 debugPos = minion->GetPosVector();
	GConsoleLogger->WriteStdOut(Color::WHITE, L"[Room::HandleMinionMove] startworld = %.3f, %.3f\n", debugPos._x, debugPos._z);

	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	shared_ptr<Navigation::WalkableGrid> gridPtr = _roomWalkableGrid.lock();
	if (navSystem == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] navSystem is nullptr\n");
		return;
	}
	if (gridPtr == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] gridPtr is nullptr\n");
		return;
	}

	Navigation::WalkableGrid& grid = *gridPtr;

	uint8 minionLaneId = minion->GetLaneId();
	shared_ptr<Navigation::LaneRoute> route = minion->GetLaneRoute().lock();
	if (route == nullptr || route->waypoints.empty())
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] lane route invalid\n");
		return;
	}

	//wpIndex = minion->GetCurrentWaypointIndex();
	if (wpIndex < 0 || wpIndex >= static_cast<int32>(route->waypoints.size()))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] minion waypoint is invalid\n");
		return;
	}

	// start cell
	const GameMath::Vector3& startPos = minion->GetPosVector();
	GameMath::Vector3 tStartPos = startPos;
	const uint8 startLane = navSystem->GetLaneId(grid, tStartPos);
	if (startLane == 0)
	{
		GConsoleLogger->WriteStdErr(Color::YELLOW,
			L"[Room::HandleMinionMove] startLane=0 (off-lane). objId=%d start=(%.2f,%.2f)\n",
			minion->GetObjectId(), startPos._x, startPos._z);
		return;
	}
	int32 sx = 0, sz = 0, tx = 0, tz = 0;
	if (!navSystem->WorldToGrid(grid, startPos._x, startPos._z, sx, sz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] WorldToGrid(start) fail\n");
		return;
	}

	// target cell (현재 waypoint)
	const GameMath::Vector3& targetPos = route->waypoints[wpIndex];
	GameMath::Vector3 tTargetPos = targetPos;
	const uint8 targetLane = navSystem->GetLaneId(grid, tTargetPos);
	if (targetLane == 0 || targetLane != minionLaneId)
	{
		GConsoleLogger->WriteStdErr(Color::YELLOW,
			L"[Room::HandleMinionMove] targetLane invalid. objId=%d wp=%d allowLane=%d targetLane=%d target=(%.2f,%.2f)\n",
			minion->GetObjectId(), wpIndex, minionLaneId, targetLane, targetPos._x, targetPos._z);
		return;
	}

	if (!navSystem->WorldToGrid(grid, targetPos._x, targetPos._z, tx, tz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] WorldToGrid(target) fail\n");
		return;
	}

	// --- A* PathFinding ---
	vector<Navigation::GridCell*> gridPath;
	GConsoleLogger->WriteStdOut(
		Color::WHITE,
		L"[Room::HandleMinionMove] sx : %d, sz : %d, tx : %d, tz : %d, laneId : %d\n",
		sx, sz, tx, tz, minionLaneId);

	bool ok = navSystem->FindPath(grid, sx, sz, tx, tz, gridPath, minionLaneId);

	GConsoleLogger->WriteStdOut(
		Color::WHITE,
		L"[Room::HandleMinionMove] FindPath ok=%d gridPathSize=%d\n",
		ok ? 1 : 0,
		static_cast<int32>(gridPath.size()));

	if (!ok || gridPath.empty())
	{
		GConsoleLogger->WriteStdErr(
			Color::YELLOW,
			L"[Room::HandleMinionMove] FindPath failed (lane filtered or no path)\n");
		return;
	}

	// --- GridPath -> NavPath (world space) ---
	// [DIAGNOSTIC] gridPath 첫/마지막 셀 좌표 출력 - 경로 방향 확인
	if (!gridPath.empty())
	{
		Navigation::GridCell* first = gridPath.front();
		Navigation::GridCell* last = gridPath.back();
		float wx0 = grid.origin._x + (first->x + 0.5f) * grid.cellSize;
		float wz0 = grid.origin._z + (first->z + 0.5f) * grid.cellSize;
		float wx1 = grid.origin._x + (last->x + 0.5f) * grid.cellSize;
		float wz1 = grid.origin._z + (last->z + 0.5f) * grid.cellSize;
		GConsoleLogger->WriteStdOut(Color::YELLOW,
			L"[DIAG] gridPath[0]=(%d,%d) world=(%.2f,%.2f)  gridPath[last]=(%d,%d) world=(%.2f,%.2f)\n",
			first->x, first->z, wx0, wz0,
			last->x, last->z, wx1, wz1);
		GConsoleLogger->WriteStdOut(Color::YELLOW,
			L"[DIAG] startPos=(%.2f,%.2f) destPos=(%.2f,%.2f) targetPos=(%.2f,%.2f)\n",
			startPos._x, startPos._z, dest._x, dest._z, targetPos._x, targetPos._z);
	}

	vector<GameMath::Vector3> navPath;
	navPath.reserve(gridPath.size());

	navPath.push_back(startPos);

	int successCount = 0;
	int failCount = 0;

	for (Navigation::GridCell* cell : gridPath)
	{
#if 1
		// 안전 버전: GridToWorld를 통하지 않고 직접 world pos 계산
		GameMath::Vector3 wp;
		wp._x = grid.origin._x + (cell->x + 0.5f) * grid.cellSize;
		wp._z = grid.origin._z + (cell->z + 0.5f) * grid.cellSize;
		wp._y = 0.0f;
		navPath.push_back(wp);
		++successCount;
#else
		// 만약 GridToWorld를 꼭 쓰고 싶다면 이 분기에서 실패/성공을 나눠서 로그
		GameMath::Vector3 wp;
		if (navSystem->GridToWorld(grid, cell->x, cell->z, wp))
		{
			navPath.push_back(wp);
			++successCount;
		}
		else
		{
			++failCount;
			const Navigation::GridCell& c = grid.At(cell->x, cell->z);
			GConsoleLogger->WriteStdErr(
				Color::RED,
				L"[Room::HandleMinionMove] GridToWorld failed for (%d,%d) walkable=%d laneId=%d\n",
				cell->x, cell->z,
				c.walkable ? 1 : 0,
				c.laneId);
		}
#endif
	}

	GConsoleLogger->WriteStdOut(
		Color::GREEN,
		L"[Room::HandleMinionMove] navPath built. success=%d fail=%d\n",
		successCount,
		failCount);

	if (navPath.empty())
	{
		GConsoleLogger->WriteStdErr(
			Color::RED,
			L"[Room::HandleMinionMove] navPath is empty AFTER conversion\n");
		return;
	}

	// 마지막에 마지막 목표점 강제 추가
	if (!navPath.empty())
		navPath.push_back(dest);
	else
		navPath.push_back(dest);

	if (navPath.empty())
		return;

	// Path Smoothing
	size_t beforeSize = navPath.size();
	navPath = SmoothPath(navPath, grid, navSystem, minionLaneId);
	GConsoleLogger->WriteStdOut(Color::GREEN, L"[SmoothPath] %zu → %zu nodes\n",
		beforeSize, navPath.size());

	// --- 미니언에 이동 경로 전달 ---
	minion->RequestMove(navPath);

	// S_MINION_MOVE 브로드캐스트 — 클라이언트에 경로 전달
	Protocol::S_MINION_MOVE minionMovePkt;
	minionMovePkt.set_object_id(minion->GetObjectId());
	minionMovePkt.set_speed(minion->GetMoveSpeed());

	Protocol::PosInfo* startPosInfo = minionMovePkt.mutable_start_pos();
	startPosInfo->set_x(minion->GetPosVector()._x);
	startPosInfo->set_y(minion->GetPosVector()._y);
	startPosInfo->set_z(minion->GetPosVector()._z);

	for (const auto& wp : navPath)
	{
		Protocol::PosInfo* pathPoint = minionMovePkt.add_nav_path();
		pathPoint->set_x(wp._x);
		pathPoint->set_y(wp._y);
		pathPoint->set_z(wp._z);
	}

	SendBufferRef minionMoveBuffer = ClientPacketHandler::MakeSendBuffer(minionMovePkt);
	Broadcast(minionMoveBuffer);
}

void Room::HandleMinionAttack(shared_ptr<Minion> attacker, shared_ptr<Object> target)
{
	if (attacker == nullptr || target == nullptr)
	{
		return;
	}

	if (target->IsDead())
		return;

	// 데미지 적용
	uint64 dmg = attacker->GetStatInfo().attack();
	bool died = target->ApplyDamage(dmg);

	GConsoleLogger->WriteStdOut(Color::GREEN,
		L"[Room::HandleMinionAttack] attacker=%d target=%d dmg=%llu died=%d\n",
		attacker->GetObjectId(), target->GetObjectId(), dmg, died);

	// S_SKILL 브로드캐스트 (클라이언트에 피격 알림)
	Protocol::S_SKILL skillPkt;
	skillPkt.set_skill_id(0);  // 0 = 기본공격
	skillPkt.set_attacker_id(attacker->GetObjectId());
	skillPkt.set_target_id(target->GetObjectId());
	Broadcast(ClientPacketHandler::MakeSendBuffer(skillPkt));

	// 사망 처리
	if (died)
	{
		target->OnDead();  // → Minion::OnDead() or Player::OnDead()
	}
}

void Room::HandleChaseMove(shared_ptr<Minion> minion, GameMath::Vector3 dest, float speed, float deltaTime, uint8 laneId)
{
	if (minion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleChaseMove] minion is nullptr\n");
		return;
	}

	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	shared_ptr<Navigation::WalkableGrid> gridPtr = _roomWalkableGrid.lock();
	if (navSystem == nullptr || gridPtr == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleChaseMove] nav is nullptr\n");
		return;
	}

	Navigation::WalkableGrid& grid = *gridPtr;

	// startCell
	const GameMath::Vector3& startPos = minion->GetPosVector();
	int32 sx = 0, sz = 0, tx = 0, tz = 0;
	if (!navSystem->WorldToGrid(grid, startPos._x, startPos._z, sx, sz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleChaseMove] WorldToGrid(start) fail\n");
		return;
	}

	// dest cell: laneId 필터 없이 허용, 플레이어의 위치는 다른 laneId일 수 있음
	GameMath::Vector3 clampedDest = dest;
	if (!navSystem->WorldToGrid(grid, clampedDest._x, clampedDest._z, tx, tz))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleChaseMove] WorldToGrid(dest) fail\n");
		return;
	}

	// A* 알고리즘 -> laneId = 0으로 레인 필터를 해제한다 -> Chase는 Lane 경계를 넘을 수 있다.
	vector<Navigation::GridCell*> gridPath;
	bool ok = navSystem->FindPath(grid, sx, sz, tx, tz, gridPath, laneId);
	if (ok == false || gridPath.empty())
	{
		GConsoleLogger->WriteStdErr(Color::YELLOW, L"[Room::HandleChaseMove] FindPath failed\n");
		return;
	}

	// GridsPath -> world navpath
	vector<GameMath::Vector3> navPath;
	navPath.reserve(gridPath.size() + 1);
	for (Navigation::GridCell* cell : gridPath)
	{
		GameMath::Vector3 wp;
		wp._x = grid.origin._x + (cell->x + 0.5f) * grid.cellSize;
		wp._z = grid.origin._z + (cell->z + 0.5f) * grid.cellSize;
		wp._y = 0.0f;
		navPath.push_back(wp);
	}
	navPath.push_back(dest);	// 마지막은 타겟의 실제 위치

	// Path Smoothing
	size_t before = navPath.size();
	navPath = SmoothPath(navPath, grid, navSystem, 0);	// 레인과 무관하게 스무딩한다
	GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::HandleChaseMove] SmoothPath %zu -> %zu\n", before, navPath.size());

	// 미니언에 경로 전달
	minion->RequestMove(navPath);

	// S_MINION_MOVE 브로드캐스트
	Protocol::S_MINION_MOVE pkt;
	pkt.set_object_id(minion->GetObjectId());
	Protocol::PosInfo* startInfo = pkt.mutable_start_pos();
	startInfo->set_x(startPos._x);
	startInfo->set_y(startPos._y);
	startInfo->set_z(startPos._z);
	for (const auto& wp : navPath)
	{
		Protocol::PosInfo* p = pkt.add_nav_path();
		p->set_x(wp._x);
		p->set_y(wp._y);
		p->set_z(wp._z);
	}
	SendBufferRef buf = ClientPacketHandler::MakeSendBuffer(pkt);
	Broadcast(buf);
}

void Room::HandleRemoveObject(int32 id)
{
	ObjectRef target = _objects.find(id)->second;
	if (target == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleRemoveObject] remove target is nullptr\n");
		return;
	}

	Protocol::S_DIE diePkt;
	diePkt.set_target_id(id);
	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(diePkt);

	_objects.erase(id);
}

void Room::BroadcastMoving(const ObjectRef& obj)
{
	// Moving Start
	Protocol::S_MOVE movePkt;
	movePkt.set_object_id(obj->GetObjectId());
	// TODO : POS는 & 형태로 가져오는 것이 유리할 것이다
	Protocol::PosInfo* pos = movePkt.mutable_server_pos_info();
	*pos = obj->GetPosInfo();
	pos->set_state(obj->GetMoveState());
	//cout << obj->GetObjectId() << " : " <<  pos->state() << endl;
	obj->OnMoveBroadcastSent(); // 타이머/플래그 리셋

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(movePkt);
	Broadcast(sendBuffer);
}

void Room::BroadcastMovingEnd(const ObjectRef& obj)
{
	// Moving End
	Protocol::S_MOVE_END endMovePkt;
	endMovePkt.set_object_id(obj->GetObjectId());
	Protocol::PosInfo* pos = endMovePkt.mutable_server_pos_info();
	*pos = obj->GetPosInfo();
	pos->set_state(obj->GetMoveState());
	//cout << pos->state() << endl;

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(endMovePkt);
	Broadcast(sendBuffer);
}

void Room::InitLaneRouteBin()
{
	// navigation system
	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	shared_ptr<Navigation::WalkableGrid> gridPtr = _roomWalkableGrid.lock();

	if (navSystem == nullptr || gridPtr == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"NavigationSystem or WalkableGrid is nullptr\n");
		return;
	}
	Navigation::WalkableGrid& grid = *gridPtr;

	// laneId 목록 수집
	unordered_set<int32> laneIds;
	laneIds.reserve(_laneIdCnt);
	for (int32 z = 0; z < grid.height; ++z)
	{
		for (int32 x = 0; x < grid.width; ++x)
		{
			const Navigation::GridCell& cell = grid.At(x, z);
			if (cell.walkable == false)
				continue;

			// invalid 타일
			if (cell.laneId <= 0)
				continue;

			laneIds.insert(cell.laneId);
		}
	}

	if (laneIds.empty() == true)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::InitLaneRoute] LaneId is invalid\n");
		return;
	}

	// laneId 별 LaneRoute 생성
	for (int32 laneId : laneIds)
	{
		shared_ptr<Navigation::LaneRoute> route = make_shared<Navigation::LaneRoute>();
		route->laneId = static_cast<uint8>(laneId);

		// 간단 휴리스틱 알고리즘 구현
		for (int32 z = 0; z < grid.height; ++z)
		{
			int32 chosenX = -1;
			for (int32 x = 0; x < grid.width; ++x)
			{
				const Navigation::GridCell& cell = grid.At(x, z);
				if (cell.walkable == false)
					continue;

				if (cell.laneId != laneId)
					continue;

				chosenX = x;
				break;
			}

			if (chosenX == -1)
				continue;

			GameMath::Vector3 wp;
			// GridToWorld 성공시에만 waypoint 추가
			if (!navSystem->GridToWorld(grid, chosenX, z, wp))
				continue;

			route->waypoints.push_back(wp);
		}

		if (route->waypoints.empty())
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[Room::InitLaneRoute] lane has no waypoints. laneId=%d\n", laneId);
			continue;
		}

		// Room의 laneRoute에 등록한다.
		SetLaneRoute(laneId, route);

		GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::InitLaneRoute] laneId=%d, waypoints=%d\n",
			laneId, static_cast<int32>(route->waypoints.size()));
	}
}

void Room::InitLaneRouteJson()
{
	// 1) NavigationSystem, Grid 유효 여부 체크 (필요하면 유지)
	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	shared_ptr<Navigation::WalkableGrid> gridPtr = _roomWalkableGrid.lock();

	if (navSystem == nullptr || gridPtr == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED,
			L"[Room::InitLaneRoute] NavigationSystem or WalkableGrid is nullptr\n");
		return;
	}

	// 2) laneRoutes.json 로드
	std::unordered_map<int32, shared_ptr<Navigation::LaneRoute>> loadedRoutes;

	// 파일 경로는 네가 실제 배포 구조에 맞춰 조정
	// 예: "./Data/laneRoutes.json" 또는 "Config/laneRoutes.json"
	std::string path = "../../GW2_Client/Assets/NavMeshExport/laneRoutes.json";

	if (!LaneRouteLoader::LoadLaneRoutesFromJson(path, loadedRoutes))
	{
		GConsoleLogger->WriteStdErr(Color::RED,
			L"[Room::InitLaneRoute] Failed to load lane routes from %S\n", path.c_str());
		return;
	}


	// 3) Room 내부 테이블에 등록
	for (auto& [laneId, route] : loadedRoutes)
	{
		SetLaneRoute(laneId, route);
	}

	GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::InitLaneRoute] lane routes initialized. count=%d\n", static_cast<int32>(_laneRoute.size()));
}

weak_ptr<Navigation::LaneRoute> Room::GetLaneRoute(int32 laneId) const
{
	auto it = _laneRoute.find(laneId);
	if (it == _laneRoute.end())
		return {};
	return it->second;
}

void Room::SetLaneRoute(int32 laneId, shared_ptr<Navigation::LaneRoute> route)
{
	_laneRoute[laneId] = move(route);
}

vector<GameMath::Vector3> Room::SmoothPath(const vector<GameMath::Vector3>& path, const Navigation::WalkableGrid& grid, shared_ptr<Navigation::NavigationSystem> navSystem, uint8 laneId)
{
	// 노드가 2개 이하면 스무딩 불필요
	if (path.size() <= 2)
		return path;

	// line of sight : 두 world 좌표 사이가 직선 통과 가능한가?
	auto lineOfSight = [&](const GameMath::Vector3& from, const GameMath::Vector3& to) -> bool
		{
			int32 x0, z0, x1, z1;
			if (!navSystem->WorldToGrid(grid, from._x, from._z, x0, z0))
				return false;
			if (!navSystem->WorldToGrid(grid, to._x, to._z, x1, z1))
				return false;

			// Bresenham 직선 래스터라이즈
			int32 dx = abs(x1 - x0);
			int32 dz = abs(z1 - z0);
			int32 sx = (x0 < x1) ? 1 : -1;
			int32 sz = (z0 < z1) ? 1 : -1;
			int32 err = dx - dz;
			int32 cx = x0, cz = z0;

			while (true)
			{
				// 그리드 범위 초과 -> 통과 불가
				if (cx < 0 || cz < 0 || cx >= grid.width || cz >= grid.height)
					return false;

				const Navigation::GridCell& cell = grid.At(cx, cz);

				// 가동 불가 지역
				if (!cell.walkable)
					return false;

				// laneId 필터
				if (laneId != 0 && cell.laneId != laneId)
					return false;

				// 목적지 도달
				if (cx == x1 && cz == z1)
					break;

				// Bresenham 진행
				int32 e2 = 2 * err;
				if (e2 > -dz)
				{
					err -= dz;
					cx += sx;
				}

				if (e2 < dx)
				{
					err += dx;
					cz += sz;
				}
			}
			return true;
		};
	// Greedy anchor 처리
	vector<GameMath::Vector3> smoothed;
	smoothed.reserve(16);
	smoothed.push_back(path[0]);

	// anchor 탐색
	size_t anchor = 0;
	while (anchor < path.size() - 1)
	{
		// anchor에서 직선으로 닿을 수 있는 가장 먼 노드를 찾는다
		size_t reach = anchor + 1;
		for (size_t i = anchor + 2; i < path.size(); ++i)
		{
			if (lineOfSight(path[anchor], path[i]))
				reach = i;
			// 레인이 꺾이는 경우를 위해 끝까지 탐색한다. -> break가 없다
		}
		smoothed.push_back(path[reach]);
		anchor = reach;
	}
	return smoothed;
}