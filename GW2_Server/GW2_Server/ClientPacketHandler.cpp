#include "pch.h"
#include "ClientPacketHandler.h"
#include "Player.h"
#include "Room.h"
#include "GameSession.h"

PacketHandlerFunc GPacketHandler[UINT16_MAX];

// ���� ������ �۾���

bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	// TODO : Log
	return false;
}

// DB는 우선 빼고, 간단하게 닉네임만 던져주자
bool Handle_C_LOGIN(PacketSessionRef& session, Protocol::C_LOGIN& pkt)
{
	cout << "LoginPacket Recv" << endl;
	cout << pkt.nickname() << endl;
	
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);

	Protocol::S_LOGIN replyLoginPkt;
	replyLoginPkt.set_success(true);
	SEND_PACKET(replyLoginPkt);

	return true;
}

bool Handle_C_ENTER_GAME(PacketSessionRef& session, Protocol::C_ENTER_GAME& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	uint64 index = pkt.playerindex();

	// TODO : Validation

	// DB ����� �׻� �ּ�ȭ �ؾ��Ѵ�
	//PlayerRef player = gameSession->_players[index];	// READ_ONLY?
	gameSession->_currentPlayer = gameSession->_players[index];	// READ ONLY
	gameSession->_room = GRoom;

	GRoom->DoAsync(&Room::Enter, gameSession->_currentPlayer);	// ���� Ŭ������ ��� �Լ��� ȣ���ϴ� ������� ����

	Protocol::S_ENTER_GAME enterGamePkt;
	enterGamePkt.set_success(true);
	auto sendBuffer = ClientPacketHandler::MakeSendBuffer(enterGamePkt);
	gameSession->_currentPlayer->ownerSession->Send(sendBuffer);

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

bool Handle_C_MOVE(PacketSessionRef& session, Protocol::C_MOVE& pkt)
{

	return true;
}

bool Handle_C_SKILL(PacketSessionRef& session, Protocol::C_SKILL& pkt)
{
	return false;
}
