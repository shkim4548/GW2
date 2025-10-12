#include "pch.h"
#include "ClientPacketHandler.h"
#include "Player.h"
#include "Room.h"
//#include "Protocol.pb.h"
#include "Struct.pb.h"
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
	Protocol::S_ENTER_GAME enterPkt;
	Protocol::ObjectInfo* objectInfo = new Protocol::ObjectInfo();
	Protocol::PosInfo* posInfo = new Protocol::PosInfo();

	objectInfo->set_creature_type(Protocol::CREATURE_TYPE_NONE);
	objectInfo->set_object_id(1);
	posInfo->set_x(0);
	posInfo->set_y(0);
	posInfo->set_z(0);
	posInfo->set_yaw(0);
	objectInfo->set_allocated_pos_info(posInfo);
	enterPkt.set_allocated_player(objectInfo);

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

bool Handle_C_MOVE_START(PacketSessionRef& session, Protocol::C_MOVE_START& pkt)
{
	const Protocol::PosInfo* startPos = &pkt.start();
	const Protocol::PosInfo* destPos = &pkt.dest();
	cout << startPos->x() << ' ' << startPos->y() << ' ' << startPos->z() << endl;

	return true;
}

bool Handle_C_MOVE_END(PacketSessionRef& session, Protocol::C_MOVE_END& pkt)
{
	return false;
}

bool Handle_C_SKILL(PacketSessionRef& session, Protocol::C_SKILL& pkt)
{
	return false;
}
