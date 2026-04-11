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
	shared_ptr<Room> room = GLobby->GetRoomById(pkt.roomid()).lock();
	if (!room)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[C_ENTER_GAME] room not found roomId=%d\n", pkt.roomid());
		return true;
	}

	int32 mode = pkt.game_mode();
	GConsoleLogger->WriteStdOut(Color::WHITE, L"[C_ENTER_GAME] mode : %d\n", mode);
	if (mode == 0) room->SetMaxPlayers(1);
	else if (mode == 1) room->SetMaxPlayers(2);
	else if (mode == 2) room->SetMaxPlayers(4);

	// playerindex 대신 session에서 직접 플레이어 조회
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	PlayerRef player = dynamic_pointer_cast<Player>(gameSession->_currentPlayer.load());
	if (player == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[C_ENTER_GAME] player is nullptr\n");
		return false;
	}

	GConsoleLogger->WriteStdOut(Color::WHITE, L"[C_ENTER_GAME] enter playerId=%d\n", player->GetPlayerId());
	//room->DoAsync(&Room::Enter, player);
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
		GConsoleLogger->WriteStdErr(Color::RED, L"[Handle_C_MOVE] room is nullptr\n");
		return false;
	}
	
	// Room을 얻어내고 해당 Room에서 player를 가져온다
	int32 playerId = pkt.object_id();
	PlayerRef player = room->GetPlayerById(playerId).lock();
	if (player == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Handle_C_MOVE] player is nullptr\n");
		return false;
	}

	//DEBUG
	// startPos
	//cout << "[Handle_C_MOVE] StartPos " << pkt.start_pos().x() << ", " << pkt.start_pos().y() << ", " << pkt.start_pos().z() << '\n';
	// targetPos
	//cout << "[Handle_C_MOVE] TargetPos " << pkt.target_pos().x() << ", " << pkt.target_pos().y() << ", " << pkt.target_pos().z() << '\n';

	room->DoAsync(&Room::HandleMovePlayer, pkt);
	return true;
}


bool Handle_C_ENTER_LOBBY(PacketSessionRef& session, Protocol::C_ENTER_LOBBY& pkt)
{
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Handle_C_ENTER_LOBBY] Enter Lobby Packet Recv\n");
	Protocol::S_ENTER_LOBBY lobbyPkt;

	unordered_map<int32, RoomRef> rooms = GLobby->GetRoomList();
	for (auto& [roomId, room] : rooms)
	{
		Protocol::RoomInfo* roomInfo = lobbyPkt.add_room_infos();
		roomInfo->set_roomid(roomId);
		roomInfo->set_rommname(room->GetRoomName());
	}

	// GLobby->OnClientEnter(newPlayer); ← 이 줄 제거

	session->Send(ClientPacketHandler::MakeSendBuffer(lobbyPkt));
	return true;
}

bool Handle_C_BUY_CARD(PacketSessionRef& session, Protocol::C_BUY_CARD& pkt)
{
	auto room = GLobby->GetRoomById(pkt.room_id()).lock();
	if (!room) return false;

	int32 playerId = static_pointer_cast<GameSession>(session)->GetPlayer()->GetPlayerId();
	PlayerRef player = room->GetPlayerById(playerId).lock();
	if (!player) return false;

	room->DoAsync(&Room::HandleBuyCard, player, pkt.card_id());
	return true;
}

bool Handle_C_REMOVE_CARD(PacketSessionRef& session, Protocol::C_REMOVE_CARD& pkt)
{
	auto room = GLobby->GetRoomById(pkt.room_id()).lock();
	if (!room) return false;

	int32 playerId = static_pointer_cast<GameSession>(session)->GetPlayer()->GetPlayerId();
	PlayerRef player = room->GetPlayerById(playerId).lock();
	if (!player) return false;

	room->DoAsync(&Room::HandleRemoveCard, player, pkt.card_id());
	return true;
}

bool Handle_C_SELECT_CHARACTER(PacketSessionRef& session, Protocol::C_SELECT_CHARACTER& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	PlayerRef player = dynamic_pointer_cast<Player>(gameSession->_currentPlayer.load());
	if (player == nullptr) 
		return false;

	shared_ptr<Room> room = GLobby->GetRoomById(pkt.room_id()).lock();
	if (room == nullptr) 
		return false;

	room->DoAsync(&Room::HandleSelectCharacter, player, pkt.player_type());
	return true;
}

bool Handle_C_CONFIRM_CHARACTER(PacketSessionRef& session, Protocol::C_CONFIRM_CHARACTER& pkt)
{
	GConsoleLogger->WriteStdOut(Color::WHITE, L"Confirm Character\n");
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	PlayerRef player = dynamic_pointer_cast<Player>(gameSession->_currentPlayer.load());
	GConsoleLogger->WriteStdOut(Color::WHITE, L"[Handle_C_CONFIRM_CHARACTER] player objectId=%d\n", player ? player->GetObjectId() : -1);

	if (player == nullptr) 
		return false;

	shared_ptr<Room> room = GLobby->GetRoomById(pkt.room_id()).lock();
	if (room == nullptr) 
		return false;

	room->DoAsync(&Room::HandleConfirmCharacter, player, pkt.player_type());	
	return true;
}

bool Handle_C_FIND_GAME(PacketSessionRef& session, Protocol::C_FIND_GAME& pkt)
{
	int32 mode = pkt.game_mode();
	int32 maxPlayers = (mode == 0) ? 1 : (mode == 1) ? 2 : 4;

	RoomRef room = GLobby->FindOrCreateRoom(mode, maxPlayers);
	if (room == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Handle_C_FIND_GAME] room is nullptr\n");
		return false;
	}

	Protocol::S_FIND_GAME replyPkt;
	replyPkt.set_room_id(room->GetRoomId());
	session->Send(ClientPacketHandler::MakeSendBuffer(replyPkt));

	GConsoleLogger->WriteStdOut(Color::WHITE, L"[Handle_C_FIND_GAME] mode=%d -> roomId=%d\n", mode, room->GetRoomId());
	return true;
}
