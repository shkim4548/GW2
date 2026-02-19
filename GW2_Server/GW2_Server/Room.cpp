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

bool Room::Enter(ObjectRef gameObject)
{
	if (gameObject == nullptr)
	{
		return false;
	}

	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[EnterGameHandler] player Enter Game Room\n");

	int32 objectId = gameObject->GetObjectId();
	_objects.emplace(objectId, gameObject);

	Protocol::S_ENTER_GAME enterPkt;
	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();

	if (shared_ptr<Player> player = static_pointer_cast<Player>(gameObject))
	{
		GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::Enter] enterPlayer\n");
		objectInfo->set_object_type(Protocol::OBJECT_TYPE_PLAYER);
		objectInfo->set_object_id(objectId);
		posInfo->set_x(72.5);
		posInfo->set_y(0);
		posInfo->set_z(0);
		posInfo->set_yaw(0);
		objectInfo->set_allocated_pos_info(posInfo);
		enterPkt.set_allocated_player(objectInfo);
		_players.emplace(objectId, player);
	}

	else if (shared_ptr<Minion> minion = static_pointer_cast<Minion>(gameObject))
	{
		GConsoleLogger->WriteStdOut(Color::GREEN, L"[Room::Enter] enterMinion\n");
		objectInfo->set_object_type(Protocol::OBJECT_TYPE_MINION);
		objectInfo->set_object_id(objectId);
		posInfo->set_x(minion->GetPosInfo().x());
		posInfo->set_y(minion->GetPosInfo().y());
		posInfo->set_z(minion->GetPosInfo().z());
		posInfo->set_yaw(0);
		objectInfo->set_allocated_pos_info(posInfo);
		enterPkt.set_allocated_player(objectInfo);
	}

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

bool Room::HandleEnterPlayer(PlayerRef player)
{
	return false;
}


bool Room::HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt)
{
	return false;
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
	// 미니언 스폰
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
			SpawnMinion(0, tempPos, Protocol::CAMP_CYBORG);
		}
	}


	for (auto& [id, obj] : _objects)
	{
		//cout << "UpodateRoom is running now" << endl;
		// 이동중이 아니라면 스킵한다.
		if (obj->GetMoveState() != Protocol::MOVE_STATE_RUN)
		{
			continue;
		}

		// Minions
		for (auto& [id, obj] : _objects)
		{
			if (obj == nullptr)
			{
				continue;
			}

			if (auto minion = dynamic_pointer_cast<Minion>(obj))
			{
				minion->UpdateMinion(deltaTime);
			}
		}

		// 이동 업데이트 한다
		bool movedThisTick = obj->UpdateMovement(deltaTime);
		obj->AccumulateMoveTime(deltaTime);
		if (!movedThisTick)
			continue;

		// 브로드 캐스트 타이밍 체크
		obj->AccumulateMoveTime(deltaTime);
		if (obj->ShouldBroadcastMove())
		{
			BroadcastMoving(obj);
			obj->ResetBroadcastTimer();
		}

		// 이동 종료 감지, 일단 이 if 로 들어오지도 않는다.
		if (obj->GetMoveState() != Protocol::MoveState::MOVE_STATE_RUN)
		{
			BroadcastMovingEnd(obj);
			//cout << "Terminate Moving" << endl;
		}

		obj->PostUpdate();
	}
}

shared_ptr<Minion> Room::SpawnMinion(int32 laneId, const GameMath::Vector3& spawnWorldPos, Protocol::CampType team)
{
	// route 확보
	weak_ptr<Navigation::LaneRoute> routeWeak = GetLaneRoute(laneId);
	shared_ptr<Navigation::LaneRoute> route = routeWeak.lock();
	if (route == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::SpawnMinion] LaneRoute missing. laneId=%d\n", laneId);
		return nullptr;
	}

	// 미니언 생성
	shared_ptr<Minion> minion = ObjectUtils::CreateMinion();
	// 기본 파라미터 세팅
	minion->SetLaneRoute(route);
	minion->_laneId = static_cast<uint8>(laneId);

	// 위치 초기화
	Protocol::PosInfo posInfo;
	posInfo.set_x(spawnWorldPos._x);
	posInfo.set_y(spawnWorldPos._y);
	posInfo.set_z(spawnWorldPos._z);
	minion->SetPosInfo(posInfo);
	
	// Room에 등록한다
	Enter(minion);
	return minion;
}

void Room::CollectEnemiesInRange(const shared_ptr<Object> requester, float range, vector<shared_ptr<Object>>& targets) const
{
	targets.clear();
	if (requester == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::CollectEnemiesInRange] requester is nullptr\n");
		return;
	}
	
	const Protocol::CampType team = requester->GetTeamFlag();
	const GameMath::Vector3 requesterPos = requester->GetPosVector();
	const float rangeSquare = range * range;
	
	// 선형탐색의 범위를 자신의 라인 안으로만 한정한다.
	const shared_ptr<Minion>& asMinion = requester->IsMinion() ? static_pointer_cast<Minion>(requester) : nullptr;
	const uint8 myLaneId = asMinion ? asMinion->_laneId : 0;

	for (auto& [id, obj] : _objects)
	{
		if (!obj || obj == requester /*|| obj->IsDead()*/)
			continue;
		if (obj->GetTeamFlag() == team)
			continue;

		// (선택) 미니언이면 같은 laneId 대상만
		if (asMinion)
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
			targets.push_back(obj);
	}
}

void Room::HandleMinionMove(shared_ptr<Minion> minion, const GameMath::Vector3& dest, float speed, float deltaTime, uint8 laneId)
{
	// 삭제/상태/권한 우선 체크
	if (minion == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] minion is nullptr");
		return;
	}

	shared_ptr<Navigation::NavigationSystem> navSystem = _navigationSystem.lock();
	if (navSystem == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] minion is nullptr");
		return;
	}

	// grid는 참조로만 가져와야한다.
	Navigation::WalkableGrid& grid = navSystem->GetGridCells();

	// Start/End world position
	GameMath::Vector3 startWorldPos = minion->GetPosVector();
	GameMath::Vector3 endWorldPos = dest;

	// WorldPos -> GridPos
	int32 sx = 0, sz = 0, tx = 0, tz = 0;
	if (navSystem->WorldToGrid(grid, startWorldPos, sx, sz) == false)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] WorldToGrid(start) fail\n");
		return;
	}
	if (navSystem->WorldToGrid(grid, endWorldPos, tx, tz) == false)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMinionMove] WorldToGrid(end) is fail\n");
		return;
	}

	// PathFinding
	vector<Navigation::GridCell*> gridPath;
	bool ok = navSystem->FindPath(grid, sx, sz, tx, tz, gridPath, laneId);
	if (ok == false || gridPath.empty())
	{
		// lane 제한 때문에 실패할 수 있음(정상 케이스도 존재)
		GConsoleLogger->WriteStdErr(Color::YELLOW, L"[Room::HandleMinionMove] FindPath failed (lane filtered?)\n");
		return;
	}

	// Grid Path -> WorldPath
	vector<GameMath::Vector3> worldPath;
	worldPath.reserve(gridPath.size() + 2);
	worldPath.push_back(startWorldPos);

	for (Navigation::GridCell* cell : gridPath)
	{
		if (cell == nullptr)
		{
			continue;
		}

		GameMath::Vector3 worldPos;
		if (navSystem->GridToWorld(grid, cell->x, cell->z, worldPos) == false)
		{
			continue;
		}

		if ((worldPos - startWorldPos).Length() < 0.01f)
			continue;

		worldPath.push_back(worldPos);
	}

	if (worldPath.empty() || (worldPath.back() - endWorldPos).Length() >= 0.01f)
	{
		worldPath.push_back(endWorldPos);
	}

	// 이동 제공
	minion->SetMoveState(Protocol::MoveState::MOVE_STATE_RUN);
	minion->_path = std::move(worldPath);
	minion->_pathIndex = 0;
	minion->SetIsMoving(true);
}

void Room::HandleMinionAttack(shared_ptr<Object> target)
{
}

void Room::BroadcastMoving(const ObjectRef& obj)
{
	// Moving Start
	Protocol::S_MOVE movePkt;
	movePkt.set_object_id(obj->GetObjectId());
	// TODO : POS는 & 형태로 가져오는 것이 유리할 것이다
	Protocol::PosInfo* pos = movePkt.mutable_server_pos_info();
	*pos = obj->GetPosInfo();

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

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(endMovePkt);
	Broadcast(sendBuffer);
}

void Room::InitLaneRoute()
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
	laneIds.reserve(_laneIdCnt);	// 하드코딩 1로 되어 있음, 테스트 라인은 하나뿜
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

void Room::DeleteRoom()
{
}
