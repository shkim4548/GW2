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
	GConsoleLogger->WriteStdOut(Color::RED, L"[Handle_Invalid] Invalid Packet");
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
	Protocol::S_ENTER_GAME enterPkt;
	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();

	objectInfo->set_object_type(Protocol::OBJECT_TYPE_PLAYER);
	objectInfo->set_object_id(1);
	posInfo->set_x(72.5);
	posInfo->set_y(2.3);
	posInfo->set_z(0);
	posInfo->set_yaw(0);
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);
	
	int targetRoom = pkt.roomid();
	weak_ptr<Room> room = GLobby->GetRoomById(targetRoom);
	
	room.lock()->InitNavigation();
	//cout << "GetRoomId : " << targetRoom << endl;
	SEND_PACKET(enterPkt);
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
	weak_ptr<Room> room = GLobby->GetRoomById(roomId);
	if (room.lock() == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Handle_C_MOVE] room is nullptr");
		return false;
	}
	
	// Room을 얻어내고 해당 Room에서 player를 가져온다
	int32 playerId = pkt.object_id();
	PlayerRef player = room.lock()->GetPlayerById(playerId).lock();
	if (player == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Handle_C_MOVE] player is nullptr");
		return false;
	}

	room.lock()->HandleMovePlayer(player, pkt);
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