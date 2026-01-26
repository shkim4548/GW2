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
        S_ENTER_GAME enterGamePkt = message as S_ENTER_GAME;
        int roomId = (int)enterGamePkt.Player.RoomId;
        
        var objectService = DI.Container.Resolve<IObjectService>();
        Debug.Log($"[PacketHandler] After OBjectService");
        // TEST : EnterGame으로 받았으면 무조건 내 플레이어 캐릭터다
        objectService.Add(enterGamePkt.Player, true);
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

        // 로그인 완료시 Lobby 입장 요청
        C_ENTER_LOBBY enterLobbyRequest = new C_ENTER_LOBBY();
        var networkService = DI.Container.Resolve<INetworkService>();
        networkService.Send(enterLobbyRequest);
    }

    public static void S_MOVEHandler(PacketSession session, IMessage message)
    {
        S_MOVE movePkt = message as S_MOVE;
        var objectService = DI.Container.Resolve<IObjectService>();

        int targetId = movePkt.ObjectId;

        //objectService.FindById()
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

    internal static void S_ENTER_LOBBYHandler(PacketSession session, IMessage message)
    {
        S_ENTER_LOBBY lobbyPkt = message as S_ENTER_LOBBY;
        var networkService = DI.Container.Resolve<INetworkService>();
        networkService.SetNetworkId(lobbyPkt.PlayerId);
        
        // DEBUG
        for(int i = 0; i< lobbyPkt.RoomInfos.Count; ++i)
        {
            Debug.Log($"[S_ENTER_LOBBY] Lobby Packet PlayerId : {lobbyPkt.PlayerId}");
            Debug.Log($"[S_ENTER_LOBBY] Lobby Packet RoomId : {lobbyPkt.RoomInfos[i].RoomId}");
            Debug.Log($"[S_ENTER_LOBBY] Lobby Packet RoomName : {lobbyPkt.RoomInfos[i].RommName}");
        }
    }

    internal static void S_MOVE_CORRECTHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }

    internal static void S_MOVE_ENDHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }

    internal static void S_MOVE_STARTHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }
}
