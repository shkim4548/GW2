#include "pch.h"
#include "Lobby.h"
#include "Room.h"
#include "Player.h"
#include "GameSession.h"
#include "NavigationSystem.h"
#include "NavmeshLoader.h"
#include "ClientPacketHandler.h"

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

bool Room::Enter(PlayerRef player)
{
	if (player == nullptr)
	{
		return false;
	}

	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[EnterGameHandler] player Enter Game Room\n");
	int32 playerId = player->GetPlayerId();
	_players.emplace(playerId, player);
	_objects.emplace(playerId, player);

	Protocol::S_ENTER_GAME enterPkt;
	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();
	objectInfo->set_object_type(Protocol::OBJECT_TYPE_PLAYER);
	objectInfo->set_object_id(playerId);
	posInfo->set_x(72.5);
	posInfo->set_y(0);
	posInfo->set_x(0);
	posInfo->set_yaw(0);
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);

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
	bool ok = _navigationSystem.lock()->FindPath(grid, sx, sz, tx, tz, gridPath);

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
	for (auto& [id, obj] : _objects)
	{
		//cout << "UpodateRoom is running now" << endl;
		// 이동중이 아니라면 스킵한다.
		if (obj->GetMoveState() != Protocol::MOVE_STATE_RUN)
		{
			continue;
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
			cout << "Terminate Moving" << endl;
		}

		obj->PostUpdate();
		// TODO : Monster Moving
	}
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
