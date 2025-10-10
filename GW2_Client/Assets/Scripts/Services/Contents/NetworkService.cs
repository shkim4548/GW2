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
}

public class NetworkService : INetworkService
{ 
    ServerSession _session = new ServerSession();

    public void Send(IMessage packet)
    {
        _session.Send(packet);
        //Debug.Log(packet.GetType());
    }

    public void Init()
    {
        // DNS (Domain Name System)
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        //IPAddress ipAddr = ipHost.AddressList[0];
        //IPAddress ipAddr = IPAddress.Parse("192.168.75.2");
        IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
        //IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);
        //IPAddress ipAddr = IPAddress.Loopback;
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);
        Connector connector = new Connector();

        connector.Connect(endPoint,
            () => { return _session; },
            1);
    }

    public void Update()
    {
        List<PacketMessage> list = PacketQueue.Instance.PopAll();
        foreach (PacketMessage packet in list)
        {
            Action<PacketSession, IMessage> handler = PacketManager.Instance.GetPacketHandler(packet.Id);
            if (handler != null)
                handler.Invoke(_session, packet.Message);
        }
        //Debug.Log("Network Service Update");
    }
}
