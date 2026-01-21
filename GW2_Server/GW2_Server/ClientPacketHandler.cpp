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
	GConsoleLogger->WriteStdOut(Color::RED, L"[Handle_Invalid] Invalid Packet\n");
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
	SEND_PACKET(replyLoginPkt);

	return true;
}

bool Handle_C_ENTER_GAME(PacketSessionRef& session, Protocol::C_ENTER_GAME& pkt)
{
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Handle_C_EnterGame] OnRecv Packet\n");
	GLobby->EnterRoom(pkt.roomid(), pkt.playerindex());
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Handle_C_EnterGame] Request player id : ");
	cout << pkt.playerindex() << endl;
	return true;
}

bool Handle_C_LEAVE_GAME(PacketSessionRef& session, Protocol::C_LEAVE_GAME& pkt)
{
	return false;
}

bool Handle_C_SPAWN(PacketSessionRef& session, Protocol::C_SPAWN& pkt)
{

	return true;
}

bool Handle_C_SKILL(PacketSessionRef& session, Protocol::C_SKILL& pkt)
{
	return false;
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
	cout << "[Handle_C_MOVE] StartPos " << pkt.object_id() << ", " << pkt.start_pos().x() << ", " << pkt.start_pos().y() << ", " << pkt.start_pos().z() << '\n';
	// targetPos
	cout << "[Handle_C_MOVE] TargetPos " << pkt.object_id() << ", " << pkt.target_pos().x() << ", " << pkt.target_pos().y() << ", " << pkt.target_pos().z() << '\n';

	room->HandleMovePlayer(player, pkt);
	return false;
}


bool Handle_C_ENTER_LOBBY(PacketSessionRef& session, Protocol::C_ENTER_LOBBY& pkt)
{
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Handle_C_ENTER_LOBBY] Enter Lobby Packet Recv\n");
	Protocol::S_ENTER_LOBBY lobbyPkt;

	unordered_map<int32, RoomRef> rooms = GLobby->GetRoomList();
	vector<int32> roomIds;
	for (auto& [roomId, room] : rooms)
	{
		Protocol::RoomInfo* roomInfo = lobbyPkt.add_roominfos();
		roomInfo->set_roomid(roomId);
		roomInfo->set_rommname(room->GetRoomName());
	}

	PlayerRef newPlayer = ObjectUtils::CreatePlayer(static_pointer_cast<GameSession>(session));
	GLobby->OnClientEnter(newPlayer);
	lobbyPkt.set_playerid(newPlayer->GetPlayerId());

	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(lobbyPkt);
	session->Send(sendBuffer);

	return true;
}