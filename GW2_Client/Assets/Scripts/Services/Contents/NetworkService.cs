using Google.Protobuf;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public interface INetworkService
{
    public void Send(IMessage packet);
    public void Init();
    public void Update();
    public void SetNetworkId(int id);
    public int GetNetworkId();
    public void SetRoomId(int roomId);
    public int GetRoomId();
    public int GetGameMode();
    public void SetGameMode(int gameMode);
}

public class NetworkService : INetworkService
{ 
    ServerSession _session = new ServerSession();
    int _networkId = -1;
    int _roomId = -1;
    int _gameMode = -1;

    public void Send(IMessage packet)
    {
        _session.Send(packet);
        //Debug.Log(packet.GetType());
    }

    public void Init()
    {
        IPAddress ipAddr = IPAddress.Parse(ConfigService.GameServerIp);
        IPEndPoint endPoint = new IPEndPoint(ipAddr, ConfigService.GameServerPort);

        Connector connector = new Connector();
        connector.Connect(endPoint, () => { return _session; }, 1);
    }

    public void Update()
    {
        List<PacketMessage> list = PacketQueue.Instance.PopAll();
        foreach (PacketMessage packet in list)
        {
            try
            {
                Action<PacketSession, IMessage> handler = PacketManager.Instance.GetPacketHandler(packet.Id);
                if (handler != null)
                    handler.Invoke(_session, packet.Message);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkService] Packet error id={packet.Id}: {e}");
            }
        }
        //Debug.Log("Network Service Update");
    }

    public void SetNetworkId(int id)
    {
        _networkId = id;
    }

    public int GetNetworkId()
    {
        return _networkId;
    }

    public void SetRoomId(int id)
    {
        _roomId = id;
    }

    public int GetRoomId()
    {
        return _roomId;
    }

    public int GetGameMode()
    {
        return _gameMode;
    }

    public void SetGameMode(int gameMode)
    {
        _gameMode = gameMode;
    }
}
