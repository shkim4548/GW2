using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

// MSG ID로 취급되던 내용을 enum으로 관리한다.
public enum PacketId : ushort
{
    //PKT_C_LOGIN = 1000,
    {%- for pkt in parser.total_pkt %}
        PKT_{{pkt.name}} = {{pkt.id}},
    {%- endfor %}
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
        {%- for pkt in parser.send_pkt %}
        _onRecv.Add((ushort)PacketId.PKT_{{pkt.name}}, MakePacket<{{pkt.name}}>);
        _handler.Add((ushort)PacketId.PKT_{{pkt.name}}, PacketHandler.{{pkt.name}}Handler);
        {%- endfor %}
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