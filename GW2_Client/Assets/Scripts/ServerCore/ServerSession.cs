using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ServerSession : PacketSession
{
    public void Send(IMessage packet)
    {
        // 패킷 이름 가져오기, _는 없애고 가져올 것
        //string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
        string msg = packet.Descriptor.Name;
        string msgName = $"PKT_{msg}";
        PacketId msgId = (PacketId)Enum.Parse(typeof(PacketId), msgName);

        // 패킷의 크기를 계산
        ushort size = (ushort)packet.CalculateSize();
        byte[] sendBuffer = new byte[size + 4]; // 내용 + 헤더(사이즈, 메시지 ID)

        // 버퍼에 사이즈와 메시지 ID를 추가
        Array.Copy(BitConverter.GetBytes((ushort)size + 4), 0, sendBuffer, 0, sizeof(ushort));
        Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));

        // 패킷 내용을 직렬화하여 버퍼에 복사
        Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);

        // 네트워크를 통해 패킷 전송
        Send(new ArraySegment<byte>(sendBuffer));
    }


    public override void OnConnected(EndPoint endPoint)
	{
		Debug.Log($"OnConnected : {endPoint}");

		PacketManager.Instance.CustomHandler = (s, m, i) =>
		{
			// 패킷 큐에 넣기만 하고 안한다.
			PacketQueue.Instance.Push(i, m);
		};
    }

    public override void OnDisconnected(EndPoint endPoint)
	{
		Debug.Log($"OnDisconnected : {endPoint}");
	}

	public override void OnRecvPacket(ArraySegment<byte> buffer)
	{
        //Debug.Log("OnRecvPacket");
        PacketManager.Instance.OnRecvPacket(this, buffer);
	}

	public override void OnSend(int numOfBytes)
	{
		//Console.WriteLine($"Transferred bytes: {numOfBytes}");
	}
}