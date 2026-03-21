#pragma once
#include "Protocol.pb.h"

using PacketHandlerFunc = std::function<bool(PacketSessionRef&, BYTE*, int32)>;
extern PacketHandlerFunc GPacketHandler[UINT16_MAX];

enum : uint16
{
	PKT_C_LOGIN = 1000,
	PKT_S_LOGIN = 1001,
	PKT_C_ENTER_GAME = 1002,
	PKT_C_LEAVE_GAME = 1003,
	PKT_S_ENTER_GAME = 1004,
	PKT_C_START_GAME = 1005,
	PKT_S_START_GAME = 1006,
	PKT_C_MOVE = 1007,
	PKT_S_MOVE = 1008,
	PKT_S_MOVE_END = 1009,
	PKT_S_MINION_MOVE = 1010,
	PKT_C_SKILL = 1011,
	PKT_S_SKILL = 1012,
	PKT_C_ENTER_LOBBY = 1013,
	PKT_S_ENTER_LOBBY = 1014,
	PKT_S_DIE = 1015,
	PKT_S_HP_CHANGE = 1016,
	PKT_S_END_GAME = 1017,
	PKT_S_RESPAWN = 1018,
};

// Custom Handlers
bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len);
	bool Handle_C_LOGIN(PacketSessionRef& session, Protocol::C_LOGIN& pkt);
	bool Handle_C_ENTER_GAME(PacketSessionRef& session, Protocol::C_ENTER_GAME& pkt);
	bool Handle_C_LEAVE_GAME(PacketSessionRef& session, Protocol::C_LEAVE_GAME& pkt);
	bool Handle_C_START_GAME(PacketSessionRef& session, Protocol::C_START_GAME& pkt);
	bool Handle_C_MOVE(PacketSessionRef& session, Protocol::C_MOVE& pkt);
	bool Handle_C_SKILL(PacketSessionRef& session, Protocol::C_SKILL& pkt);
	bool Handle_C_ENTER_LOBBY(PacketSessionRef& session, Protocol::C_ENTER_LOBBY& pkt);

class ClientPacketHandler
{
public:
	static void Init()
	{
		for (int32 i = 0; i < UINT16_MAX; i++)
			GPacketHandler[i] = Handle_INVALID;
		GPacketHandler[PKT_C_LOGIN] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_LOGIN>(Handle_C_LOGIN, session, buffer, len); };
		GPacketHandler[PKT_C_ENTER_GAME] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_ENTER_GAME>(Handle_C_ENTER_GAME, session, buffer, len); };
		GPacketHandler[PKT_C_LEAVE_GAME] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_LEAVE_GAME>(Handle_C_LEAVE_GAME, session, buffer, len); };
		GPacketHandler[PKT_C_START_GAME] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_START_GAME>(Handle_C_START_GAME, session, buffer, len); };
		GPacketHandler[PKT_C_MOVE] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_MOVE>(Handle_C_MOVE, session, buffer, len); };
		GPacketHandler[PKT_C_SKILL] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_SKILL>(Handle_C_SKILL, session, buffer, len); };
		GPacketHandler[PKT_C_ENTER_LOBBY] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_ENTER_LOBBY>(Handle_C_ENTER_LOBBY, session, buffer, len); };
	}

	static bool HandlePacket(PacketSessionRef& session, BYTE* buffer, int32 len)
	{
		PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
		return GPacketHandler[header->id](session, buffer, len);
	}
	static SendBufferRef MakeSendBuffer(Protocol::S_LOGIN& pkt) { return MakeSendBuffer(pkt, PKT_S_LOGIN); }
	static SendBufferRef MakeSendBuffer(Protocol::S_ENTER_GAME& pkt) { return MakeSendBuffer(pkt, PKT_S_ENTER_GAME); }
	static SendBufferRef MakeSendBuffer(Protocol::S_START_GAME& pkt) { return MakeSendBuffer(pkt, PKT_S_START_GAME); }
	static SendBufferRef MakeSendBuffer(Protocol::S_MOVE& pkt) { return MakeSendBuffer(pkt, PKT_S_MOVE); }
	static SendBufferRef MakeSendBuffer(Protocol::S_MOVE_END& pkt) { return MakeSendBuffer(pkt, PKT_S_MOVE_END); }
	static SendBufferRef MakeSendBuffer(Protocol::S_MINION_MOVE& pkt) { return MakeSendBuffer(pkt, PKT_S_MINION_MOVE); }
	static SendBufferRef MakeSendBuffer(Protocol::S_SKILL& pkt) { return MakeSendBuffer(pkt, PKT_S_SKILL); }
	static SendBufferRef MakeSendBuffer(Protocol::S_ENTER_LOBBY& pkt) { return MakeSendBuffer(pkt, PKT_S_ENTER_LOBBY); }
	static SendBufferRef MakeSendBuffer(Protocol::S_DIE& pkt) { return MakeSendBuffer(pkt, PKT_S_DIE); }
	static SendBufferRef MakeSendBuffer(Protocol::S_HP_CHANGE& pkt) { return MakeSendBuffer(pkt, PKT_S_HP_CHANGE); }
	static SendBufferRef MakeSendBuffer(Protocol::S_END_GAME& pkt) { return MakeSendBuffer(pkt, PKT_S_END_GAME); }
	static SendBufferRef MakeSendBuffer(Protocol::S_RESPAWN& pkt) { return MakeSendBuffer(pkt, PKT_S_RESPAWN); }

private:
	template<typename PacketType, typename ProcessFunc>
	static bool HandlePacket(ProcessFunc func, PacketSessionRef& session, BYTE* buffer, int32 len)
	{
		PacketType pkt;
		if (pkt.ParseFromArray(buffer + sizeof(PacketHeader), len - sizeof(PacketHeader)) == false)
			return false;

		return func(session, pkt);
	}

	template<typename T>
	static SendBufferRef MakeSendBuffer(T& pkt, uint16 pktId)
	{
		const uint16 dataSize = static_cast<uint16>(pkt.ByteSizeLong());
		const uint16 packetSize = dataSize + sizeof(PacketHeader);

		SendBufferRef sendBuffer = GSendBufferManager->Open(packetSize);
		PacketHeader* header = reinterpret_cast<PacketHeader*>(sendBuffer->Buffer());
		header->size = packetSize;
		header->id = pktId;
		ASSERT_CRASH(pkt.SerializeToArray(&header[1], dataSize));
		sendBuffer->Close(packetSize);

		return sendBuffer;
	}
};