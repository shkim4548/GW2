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

void Room::InitNavigation()
{
	
}

bool Room::HandleEnterPlayer(PlayerRef player)
{
	return false;
}


void Room::HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt)
{
}

bool Room::HandleMovePlayer(PlayerRef player, Protocol::C_MOVE movePkt)
{
	// 이제 검증작업을 시작한다.
	constexpr float START_POS_TOLERANCE = 0.5f;
	Protocol::PosInfo startPos = movePkt.start_pos();
	GameMath::Vector3 startPosVector(startPos.x(), startPos.y(), startPos.z());
	Protocol::PosInfo endPos = movePkt.target_pos();
	GameMath::Vector3 endPosVector(endPos.x(), endPos.y(), endPos.z());

	float dist = player->GetPosVector().GetDistance(startPosVector);
	if (dist > START_POS_TOLERANCE)
	{
		// 검증 결과 부적합 -> 서버의 기준위치로 강제로 되돌린다.
		player->SetPosInfo(player->GetPosInfo());
		return false;
	}

	// 이제 Navigation System을 사용해보자
	int32 sx, sz, tx, tz;
	if (!_navigationSystem.lock()->WorldToGrid(startPosVector, sx, sz) || !_navigationSystem.lock()->WorldToGrid(endPosVector, tx, tz))
	{
		player->SetPosInfo(player->GetPosInfo());
		return false;
	}

	// A Star 알고리즘
	vector<Navigation::GridCell*> path;
	bool isPath = _navigationSystem.lock()->FindPath(sx, sz, tx, tz, path);
	//auto path = _navigationSystem.FindPath(sx, sz, tx, tz);
	if (isPath == false)
	{
		player->SetPosInfo(player->GetPosInfo());
		return false;
	}

	// 서버의 이동 승인
	player->SetPath(path);
	//_navigationSystem.lock()->GridToWorld(path.back().x, path.back().z);
	return true;
}
