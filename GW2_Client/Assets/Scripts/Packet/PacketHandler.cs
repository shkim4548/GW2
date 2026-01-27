using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.Struct;
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
        Debug.Log($"[PacketHandler] After ObjectService");
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
        networkService.SetNetworkId(recvLoginpkt.PlayerIndex);
        networkService.Send(enterLobbyRequest);
    }

    public static void S_MOVEHandler(PacketSession session, IMessage message)
    {
        S_MOVE movePkt = message as S_MOVE;
        var objectService = DI.Container.Resolve<IObjectService>();

        int targetId = movePkt.ObjectId;
        GameObject go = objectService.FindById(targetId);
        if(go == null)
        {
            Debug.Log($"[S_MOVEHandler] : objectService findById is nullptr");
            return;
        }

        // 내꺼는 수신하지 않는다 -> 이게 맞는가는 다시한번 체크해봐야함
        if(objectService.MyPlayer.Id == movePkt.ObjectId)
        {
            return;
        }

        //
        BaseController bc = go. GetComponent<BaseController>();
        if(bc == null)
        {
            Debug.Log($"[S_MOVEHandler] : bc is nullptr, type casting faileds");
            return;
        }

        PosInfo pos = new PosInfo();
        pos.X = movePkt.ServerPosInfo.X;
        pos.Y = movePkt.ServerPosInfo.Y;
        pos.Z = movePkt.ServerPosInfo.Z;

        // 서버의 권위있는 정보를 전달
        bc.PosInfo = pos;
        bc.LastServerTime = movePkt.ServerTime;
        bc._isMoving = true;
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

    public static void S_ENTER_LOBBYHandler(PacketSession session, IMessage message)
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

    public static void S_MOVE_ENDHandler(PacketSession session, IMessage message)
    {
        // EndOfMoving Recv
        S_MOVE_END endMovePkt = message as S_MOVE_END;
        IObjectService objectService = DI.Container.Resolve<IObjectService>();
        
        int targetId = endMovePkt.ObjectId;
        GameObject go = objectService.FindById(endMovePkt.ObjectId);
        BaseController bc = go.GetComponent<BaseController>();
        bc.PosInfo = endMovePkt.FinalPos;
        bc._isMoving = false;

        // 여기서는 스냅이 허용된다
        bc.transform.position.Set(bc.PosInfo.X, bc.PosInfo.Y, bc.PosInfo.Z);
        // TODO: STATE 변경 + ROTATION 변경
    }

    internal static void S_MOVE_STARTHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }
}
