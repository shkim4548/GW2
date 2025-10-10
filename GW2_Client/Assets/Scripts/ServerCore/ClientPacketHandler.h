// Custom Handlers
bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len);
    struct S_Chat; // 직접 정의된 struct (ex: C_Chat)
    bool Handle_S_Chat(PacketSessionRef& session, S_Chat& pkt);

class ClientPacketHandler
{
public:
    static void Init()
    {
        for (int32 i = 0; i < UINT16_MAX; i++)
            GPacketHandler[i] = Handle_INVALID;
        GPacketHandler[PKT_S_Chat] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {
            return HandlePacket<S_Chat>(Handle_S_Chat, session, buffer, len);
        };
    }

    static bool HandlePacket(PacketSessionRef& session, BYTE* buffer, int32 len)
    {
        PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
        return GPacketHandler[header->id](session, buffer, len);
    }
    static SendBufferRef MakeSendBuffer(C_Chat& pkt) { return MakeSendBuffer(pkt, PKT_C_Chat); }

private:
    template<typename PacketType, typename ProcessFunc>
    static bool HandlePacket(ProcessFunc func, PacketSessionRef& session, BYTE* buffer, int32 len)
    {
        PacketType pkt;
        if (pkt.Deserialize(buffer + sizeof(PacketHeader), len - sizeof(PacketHeader)) == false)
            return false;

        return func(session, pkt);
    }

    template<typename T>
    static SendBufferRef MakeSendBuffer(T& pkt, uint16 pktId)
    {
        BYTE temp[4096]; // 임시 직렬화 버퍼
        int32 written = 0;
        if (pkt.Serialize(temp, sizeof(temp), written) == false)
            return nullptr;

        const uint16 packetSize = written + sizeof(PacketHeader);

        SendBufferRef sendBuffer = GSendBufferManager->Open(packetSize);
        PacketHeader* header = reinterpret_cast<PacketHeader*>(sendBuffer->Buffer());
        header->size = packetSize;
        header->id = pktId;

        ::memcpy(&header[1], temp, written);
        sendBuffer->Close(packetSize);

        return sendBuffer;
    }
};