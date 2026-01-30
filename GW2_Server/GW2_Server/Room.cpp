#include "pch.h"
#include "Room.h"
#include "Player.h"
#include "GameSession.h"
#include "NavigationSystem.h"
#include "NavmeshLoader.h"
#include "ClientPacketHandler.h"
#include "Lobby.h"

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
	posInfo->set_y(2.3);
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


void Room::HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt)
{

}

void Room::HandleMovePlayer(Protocol::C_MOVE movePkt)
{
	Protocol::PosInfo startPos = movePkt.start_pos();
	Protocol::PosInfo endPos = movePkt.target_pos();

	GameMath::Vector3 startWorld(startPos.x(), startPos.y(), startPos.z());
	GameMath::Vector3 endWorld(endPos.x(), endPos.y(), endPos.z());

	cout << "[Room::HandleMovePlayer] Before FindPath" << endl;
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
		return;
	}

	int32 playerId = movePkt.object_id();
	weak_ptr<Player> player = _players[playerId];
	if (player.lock() == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Room::HandleMovePlayer] player is nullptr\n");
		return;
	}
	cout << "End of HandleMovePlayer" << endl;
	HandleMovePlayerInternal(player.lock(), gridPath);
}

// Path를 player에 할당한다.
void Room::HandleMovePlayerInternal(PlayerRef player, vector<Navigation::GridCell*>& gridPath)
{
	vector<GameMath::Vector3> worldPath;
	worldPath.reserve(gridPath.size());

	for (Navigation::GridCell* cell : gridPath)
	{
		GameMath::Vector3 worldPos;
		if (!_navigationSystem.lock()->GridToWorld(_navigationSystem.lock()->GetGridCells(), cell->x, cell->z, worldPos))
			continue;

		worldPath.push_back(worldPos);
	}

	if (player == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[HandleMovePlayerInternal] player is nullptr");
		return;
	}

	player->_path = move(worldPath);
	player->_pathIndex = 0;

}

void Room::UpdateRoom(float deltaTime)
{
	for (auto& [id, obj] : _objects)
	{
		bool movedThisTick = obj->UpdateMovement(deltaTime);
		obj->AccumulateMoveTime(deltaTime);
		if (movedThisTick)
		{
			BroadcastMoving(obj);
			obj->ResetBroadcastTimer();
		}

		// 이동 종료 감지
		if (obj->GetIsMoving() && obj->GetMoveState() == Protocol::MoveState::MOVE_STATE_IDLE)
		{
			BroadcastMovingEnd(obj);
		}

		obj->PostUpdate();
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
	Protocol::PosInfo* pos = endMovePkt.mutable_final_pos();
	*pos = obj->GetPosInfo();

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(endMovePkt);
	Broadcast(sendBuffer);
}
