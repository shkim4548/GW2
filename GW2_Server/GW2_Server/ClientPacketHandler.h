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
	PKT_S_SPAWN = 1005,
	PKT_C_SPAWN = 1006,
	PKT_C_MOVE_START = 1007,
	PKT_C_MOVE_END = 1008,
	PKT_S_MOVE_START = 1009,
	PKT_S_MOVE_END = 1010,
	PKT_S_MOVE_CORRECT = 1011,
	PKT_C_SKILL = 1012,
	PKT_S_SKILL = 1013,
	PKT_C_ENTER_LOBBY = 1014,
	PKT_S_ENTER_LOBBY = 1015,
};

// Custom Handlers
bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len);
bool Handle_C_LOGIN(PacketSessionRef& session, Protocol::C_LOGIN& pkt);
bool Handle_C_ENTER_GAME(PacketSessionRef& session, Protocol::C_ENTER_GAME& pkt);
bool Handle_C_LEAVE_GAME(PacketSessionRef& session, Protocol::C_LEAVE_GAME& pkt);
bool Handle_C_SPAWN(PacketSessionRef& session, Protocol::C_SPAWN& pkt);
bool Handle_C_MOVE_START(PacketSessionRef& session, Protocol::C_MOVE_START& pkt);
bool Handle_C_MOVE_END(PacketSessionRef& session, Protocol::C_MOVE_END& pkt);
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
		GPacketHandler[PKT_C_SPAWN] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_SPAWN>(Handle_C_SPAWN, session, buffer, len); };
		GPacketHandler[PKT_C_MOVE_START] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_MOVE_START>(Handle_C_MOVE_START, session, buffer, len); };
		GPacketHandler[PKT_C_MOVE_END] = [](PacketSessionRef& session, BYTE* buffer, int32 len) { return HandlePacket<Protocol::C_MOVE_END>(Handle_C_MOVE_END, session, buffer, len); };
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
	static SendBufferRef MakeSendBuffer(Protocol::S_SPAWN& pkt) { return MakeSendBuffer(pkt, PKT_S_SPAWN); }
	static SendBufferRef MakeSendBuffer(Protocol::S_MOVE_START& pkt) { return MakeSendBuffer(pkt, PKT_S_MOVE_START); }
	static SendBufferRef MakeSendBuffer(Protocol::S_MOVE_END& pkt) { return MakeSendBuffer(pkt, PKT_S_MOVE_END); }
	static SendBufferRef MakeSendBuffer(Protocol::S_MOVE_CORRECT& pkt) { return MakeSendBuffer(pkt, PKT_S_MOVE_CORRECT); }
	static SendBufferRef MakeSendBuffer(Protocol::S_SKILL& pkt) { return MakeSendBuffer(pkt, PKT_S_SKILL); }
	static SendBufferRef MakeSendBuffer(Protocol::S_ENTER_LOBBY& pkt) { return MakeSendBuffer(pkt, PKT_S_ENTER_LOBBY); }

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