using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacketHandler
{
    public static void S_ENTER_GAMEHandler(PacketSession session, IMessage message)
    {
        
    }

    public static void S_LOGINHandler(PacketSession session, IMessage message)
    {
        S_LOGIN recvLoginpkt = message as S_LOGIN;
        Debug.Log($"S_LOGINHandler : {recvLoginpkt.Success}");

        if (recvLoginpkt.Success == false)
            return;
        
        // 버튼 콜백등 호출 빈도가 낮은 부분은 Lazy Resolve
        var sceneService = DI.Container.Resolve<ISceneService>();
        sceneService.LoadScene(Define.Scene.Lobby);
    }

    public static void S_MOVEHandler(PacketSession session, IMessage message)
    {

    }

    public static void S_SKILLHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }

    public static void S_SPAWNHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }

    public static void S_TESTHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }
}
