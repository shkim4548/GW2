using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

// MSG ID로 취급되던 내용을 enum으로 관리한다.
public enum PacketId : ushort
{
    //PKT_C_LOGIN = 1000,
        PKT_C_LOGIN = 1000,
        PKT_S_LOGIN = 1001,
        PKT_C_ENTER_GAME = 1002,
        PKT_C_LEAVE_GAME = 1003,
        PKT_S_ENTER_GAME = 1004,
        PKT_S_SPAWN = 1005,
        PKT_C_SPAWN = 1006,
        PKT_C_MOVE = 1007,
        PKT_S_MOVE = 1008,
        PKT_S_MOVE_END = 1009,
        PKT_S_MINION_MOVE = 1010,
        PKT_C_SKILL = 1011,
        PKT_S_SKILL = 1012,
        PKT_C_ENTER_LOBBY = 1013,
        PKT_S_ENTER_LOBBY = 1014,
        PKT_C_ATTACK = 1015,
        PKT_S_ATTACK = 1016,
}

public class PacketManager
{
    #region Singleton
    static PacketManager _instance = new PacketManager();
    public static PacketManager Instance { get { return _instance; } }
    #endregion

    Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv = new Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>>();
    Dictionary<ushort, Action<PacketSession, IMessage>> _handler = new Dictionary<ushort, Action<PacketSession, IMessage>>();

    public Action<PacketSession, IMessage, ushort> CustomHandler { get; set; }

    PacketManager()
    {
        Register();
    }

    public void Register()
    {
        //_onRecv.Add((ushort)PacketId.PKT_S_LOGIN, MakePacket<S_LOGIN>);
        //_handler.Add((ushort)PacketId.PKT_S_LOGIN, PacketHandler.S_LOGINHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_LOGIN, MakePacket<S_LOGIN>);
        _handler.Add((ushort)PacketId.PKT_S_LOGIN, PacketHandler.S_LOGINHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_ENTER_GAME, MakePacket<S_ENTER_GAME>);
        _handler.Add((ushort)PacketId.PKT_S_ENTER_GAME, PacketHandler.S_ENTER_GAMEHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_SPAWN, MakePacket<S_SPAWN>);
        _handler.Add((ushort)PacketId.PKT_S_SPAWN, PacketHandler.S_SPAWNHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_MOVE, MakePacket<S_MOVE>);
        _handler.Add((ushort)PacketId.PKT_S_MOVE, PacketHandler.S_MOVEHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_MOVE_END, MakePacket<S_MOVE_END>);
        _handler.Add((ushort)PacketId.PKT_S_MOVE_END, PacketHandler.S_MOVE_ENDHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_MINION_MOVE, MakePacket<S_MINION_MOVE>);
        _handler.Add((ushort)PacketId.PKT_S_MINION_MOVE, PacketHandler.S_MINION_MOVEHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_SKILL, MakePacket<S_SKILL>);
        _handler.Add((ushort)PacketId.PKT_S_SKILL, PacketHandler.S_SKILLHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_ENTER_LOBBY, MakePacket<S_ENTER_LOBBY>);
        _handler.Add((ushort)PacketId.PKT_S_ENTER_LOBBY, PacketHandler.S_ENTER_LOBBYHandler);
        _onRecv.Add((ushort)PacketId.PKT_S_ATTACK, MakePacket<S_ATTACK>);
        _handler.Add((ushort)PacketId.PKT_S_ATTACK, PacketHandler.S_ATTACKHandler);
    }

    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
    {
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        Action<PacketSession, ArraySegment<byte>, ushort> action = null;
        if (_onRecv.TryGetValue(id, out action))
            action.Invoke(session, buffer, id);
    }

    void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
    {
        T pkt = new T();
        pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

        if (CustomHandler != null)
        {
            CustomHandler.Invoke(session, pkt, id);
        }
        else
        {
            Action<PacketSession, IMessage> action = null;
            if (_handler.TryGetValue(id, out action))
                action.Invoke(session, pkt);
        }
    }

    public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
    {
        Action<PacketSession, IMessage> action = null;
        if (_handler.TryGetValue(id, out action))
            return action;
        return null;
    }

    private bool Handle_INVALID(PacketSession session, byte[] buffer, int length)
    {
        // Invalid 패킷 처리 로직
        return false;
    }
}