#include "pch.h"
#include "ClientPacketHandler.h"
#include "Player.h"
#include "Room.h"
#include "Lobby.h"
#include "ObjectUtils.h"
#include "Protocol.pb.h"
#include "Struct.pb.h"
#include "GameSession.h"

PacketHandlerFunc GPacketHandler[UINT16_MAX];

// ���� ������ �۾���

bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	GConsoleLogger->WriteStdOut(Color::RED, L"[Handle_Invalid] Invalid Packet id : ");
	cout << header->id << endl;
	return false;
}

// DB는 우선 빼고, 간단하게 닉네임만 던져주자
bool Handle_C_LOGIN(PacketSessionRef& session, Protocol::C_LOGIN& pkt)
{
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Handle_C_Login] LoginPacketRecv : ");
	cout << pkt.nickname() << endl;
	
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);

	Protocol::S_LOGIN replyLoginPkt;
	replyLoginPkt.set_success(true);

	PlayerRef newPlayer = ObjectUtils::CreatePlayer(static_pointer_cast<GameSession>(session));
	GLobby->OnClientEnter(newPlayer);
	replyLoginPkt.set_player_index(newPlayer->GetPlayerId());

	SEND_PACKET(replyLoginPkt);

	return true;
}

bool Handle_C_ENTER_GAME(PacketSessionRef& session, Protocol::C_ENTER_GAME& pkt)
{
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Handle_C_EnterGame] OnRecv Packet\n");
	GLobby->EnterRoom(pkt.roomid(), pkt.playerindex());
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Handle_C_EnterGame] Request player id : ");
	cout << pkt.playerindex() << endl;
	// TEMP : For test
	shared_ptr<Room> room = GLobby->GetRoomById(pkt.roomid()).lock();
	room->SetIsRunning(true);
	return true;
}

bool Handle_C_LEAVE_GAME(PacketSessionRef& session, Protocol::C_LEAVE_GAME& pkt)
{
	return false;
}

bool Handle_C_START_GAME(PacketSessionRef& session, Protocol::C_START_GAME& pkt)
{
	return false;
}

bool Handle_C_SKILL(PacketSessionRef& session, Protocol::C_SKILL& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	//GConsoleLogger->WriteStdOut(Color::WHITE, L"[Handle_C_SKILL] C_SKILL is recved\n");
	//auto room = gameSession->_room.lock();
	shared_ptr<Room> room = GLobby->GetRoomById(pkt.room_id()).lock();
	if (room == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Handle_C_SKILL] room is nullptr\n");
		return false;
	}

	shared_ptr<Object> attacker = gameSession->_currentPlayer.load();
	room->DoAsync(&Room::HandleSkill, attacker, pkt);
	return true;
}

bool Handle_C_MOVE(PacketSessionRef& session, Protocol::C_MOVE& pkt)
{
	int32 roomId = pkt.room_id();
	auto room = GLobby->GetRoomById(roomId).lock();
	if (!room)
	{
		// 현재 에러가 발생하는 부분은 여기거든
		//cout << roomId << endl;
		GConsoleLogger->WriteStdErr(Color::RED, L"[Handle_C_MOVE] room is nullptr");
		return false;
	}
	
	// Room을 얻어내고 해당 Room에서 player를 가져온다
	int32 playerId = pkt.object_id();
	PlayerRef player = room->GetPlayerById(playerId).lock();
	if (player == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Handle_C_MOVE] player is nullptr");
		return false;
	}

	//DEBUG
	// startPos
	cout << "[Handle_C_MOVE] StartPos " << pkt.start_pos().x() << ", " << pkt.start_pos().y() << ", " << pkt.start_pos().z() << '\n';
	// targetPos
	cout << "[Handle_C_MOVE] TargetPos " << pkt.target_pos().x() << ", " << pkt.target_pos().y() << ", " << pkt.target_pos().z() << '\n';

	room->DoAsync(&Room::HandleMovePlayer, pkt);
	//room->HandleMovePlayer(player, pkt);
	return true;
}


bool Handle_C_ENTER_LOBBY(PacketSessionRef& session, Protocol::C_ENTER_LOBBY& pkt)
{
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Handle_C_ENTER_LOBBY] Enter Lobby Packet Recv\n");
	Protocol::S_ENTER_LOBBY lobbyPkt;

	unordered_map<int32, RoomRef> rooms = GLobby->GetRoomList();
	vector<int32> roomIds;
	for (auto& [roomId, room] : rooms)
	{
		Protocol::RoomInfo* roomInfo = lobbyPkt.add_room_infos();
		roomInfo->set_roomid(roomId);
		roomInfo->set_rommname(room->GetRoomName());
	}
	// TODO : RoomId HardCoding
	PlayerRef newPlayer =  GLobby->GetLobbyPlayers()[1];
	lobbyPkt.set_player_id(newPlayer->GetPlayerId());
	cout << newPlayer->GetPlayerId() << endl;

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(lobbyPkt);
	session->Send(sendBuffer);

	return true;
}