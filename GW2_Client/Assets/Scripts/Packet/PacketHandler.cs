using Google.Protobuf;
using Google.Protobuf.Enum;
using Google.Protobuf.Protocol;
using Google.Protobuf.Struct;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;

public class PacketHandler
{
    public static void S_ENTER_GAMEHandler(PacketSession session, IMessage message)
    {
        S_ENTER_GAME enterGamePkt = message as S_ENTER_GAME;
        int roomId = (int)enterGamePkt.Player.RoomId;

        var objectService = Bootstrapper.Instance.ObjectService;
        Debug.Log($"[PacketHandler] After ObjectService");
        objectService.Add(enterGamePkt.Player, true);

        // ★ 스폰 후 서버 PosInfo를 transform.position에 즉시 반영
        //   objectService.Add()가 Prefab 기본 위치로 생성하므로 명시적으로 세팅
        int objectId = enterGamePkt.Player.ObjectId;
        GameObject go = objectService.FindById(objectId);
        if (go != null)
        {
            PosInfo spawnPos = enterGamePkt.Player.PosInfo;
            Vector3 worldPos = new Vector3(spawnPos.X, spawnPos.Y, spawnPos.Z);
            go.transform.position = worldPos;

            // BaseController PosInfo도 동기화
            BaseController bc = go.GetComponent<BaseController>();
            if (bc != null)
                bc.PosInfo = spawnPos;

            Debug.Log($"[S_ENTER_GAMEHandler] objectId={objectId} " +
                      $"spawnPos=({worldPos.x:F2},{worldPos.y:F2},{worldPos.z:F2})");
        }
        else
        {
            Debug.LogWarning($"[S_ENTER_GAMEHandler] objectId={objectId} not found after Add()");
        }
    }

    public static void S_LOGINHandler(PacketSession session, IMessage message)
    {
        S_LOGIN recvLoginpkt = message as S_LOGIN;
        Debug.Log($"S_LOGINHandler : {recvLoginpkt.Success}");

        if (recvLoginpkt.Success == false)
            return;

        // 로그인 완료시 Lobby 입장 요청
        C_ENTER_LOBBY enterLobbyRequest = new C_ENTER_LOBBY();
        //var networkService = DI.Container.Resolve<INetworkService>();
        var networkService = Bootstrapper.Instance.NetworkService;
        networkService.SetNetworkId(recvLoginpkt.PlayerIndex);
        Debug.Log(networkService.GetNetworkId());
        networkService.Send(enterLobbyRequest);

        // 버튼 콜백등 호출 빈도가 낮은 부분은 Lazy Resolve
        //var sceneService = DI.Container.Resolve<ISceneService>();
        var sceneService = Bootstrapper.Instance.SceneService;
        sceneService.LoadScene(Define.Scene.Lobby);
    }

    public static void S_MOVEHandler(PacketSession session, IMessage message)
    {
        S_MOVE movePkt = message as S_MOVE;
        var objectService = Bootstrapper.Instance.ObjectService;

        int targetId = movePkt.ObjectId;
        GameObject go = objectService.FindById(targetId);
        if (go == null)
        {
            Debug.Log($"[S_MOVEHandler] objectService findById is null. id={targetId}");
            return;
        }

        // 내 플레이어는 처리하지 않음
        if (objectService.MyPlayer.Id == movePkt.ObjectId)
            return;

        BaseController bc = go.GetComponent<BaseController>();
        if (bc == null)
        {
            Debug.Log($"[S_MOVEHandler] BaseController not found. id={targetId}");
            return;
        }

        // ★ Phase 1-B : 미니언은 S_MOVE가 navPath 이동을 간섭하지 않도록 분기
        //   - PosInfo / State / _isMoving 을 덮어쓰지 않음
        //   - 서버 기준 위치만 SetServerRefPos()로 전달 (Phase 2 보정에서 사용)
        MinionController mc = bc as MinionController;
        if (mc != null)
        {
            Vector3 serverPos = new Vector3(
                movePkt.ServerPosInfo.X,
                movePkt.ServerPosInfo.Y,
                movePkt.ServerPosInfo.Z);

            mc.SetServerRefPos(serverPos, movePkt.ServerTime);

            //Debug.Log($"[S_MOVEHandler] Minion {targetId} " +
            //          $"serverRefPos=({serverPos.x:F2},{serverPos.z:F2}) t={movePkt.ServerTime}");
            return; // ← 여기서 반드시 return — navPath 이동에 일절 간섭하지 않음
        }

        // 플레이어 처리 (기존 로직 그대로)
        PosInfo pos = new PosInfo();
        pos.X = movePkt.ServerPosInfo.X;
        pos.Y = movePkt.ServerPosInfo.Y;
        pos.Z = movePkt.ServerPosInfo.Z;

        bc.PosInfo = pos;
        bc.LastServerTime = movePkt.ServerTime;
        bc._isMoving = true;
        bc.State = movePkt.ServerPosInfo.State;
        //Debug.Log($"[S_MOVEHandler] Player {targetId} " +
        //          $"serverPos=({pos.X:F2},{pos.Y:F2},{pos.Z:F2}) state={movePkt.ServerPosInfo.State}");
    }

    public static void S_SKILLHandler(PacketSession session, IMessage message)
    {
        S_SKILL skillPkt = message as S_SKILL;
        IObjectService objectService = Bootstrapper.Instance.ObjectService;

        GameObject attacker = objectService.FindById((int)skillPkt.AttackerId);
        GameObject target = objectService.FindById((int)skillPkt.TargetId);

        if (attacker == null) return;

        // 터렛 공격
        TurretController tc = attacker.GetComponent<TurretController>();
        if (tc != null)
        {
            tc.OnAttack((int)skillPkt.TargetId);
            return;
        }

        // attacker State 변경 (애니메이션용)
        BaseController attackerBc = attacker.GetComponent<BaseController>();
        if (attackerBc == null) return;

        attackerBc.State = Google.Protobuf.Enum.MoveState.Skill;

        // 내 플레이어가 공격한 경우 → 이펙트 이미 재생했으므로 스킵
        if (objectService.MyPlayer != null &&
            objectService.MyPlayer.Id == (int)skillPkt.AttackerId)
            return;

        // 상대 플레이어/미니언이 공격한 경우 → 이펙트 재생
        // target이 이미 삭제됐을 수 있으므로 null 허용
        attackerBc.PlayAttackEffect(target);
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
        var networkService = Bootstrapper.Instance.NetworkService;
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
        S_MOVE_END endMovePkt = message as S_MOVE_END;
        IObjectService objectService = Bootstrapper.Instance.ObjectService;

        int targetId = endMovePkt.ObjectId;
        GameObject go = objectService.FindById(targetId);
        if (go == null)
        {
            Debug.LogWarning($"[S_MOVE_END] objectId={targetId} not found");
            return;
        }

        BaseController bc = go.GetComponent<BaseController>();
        if (bc == null)
        {
            Debug.LogWarning($"[S_MOVE_END] BaseController not found. id={targetId}");
            return;
        }

        // 옵션 A : 미니언은 navPath가 자체적으로 ArriveAtDestination()을 처리한다.
        // S_MOVE_END로 위치/상태를 강제 덮어쓰면 경로 재생 도중 강제 정지되므로 분기.
        MinionController mc = bc as MinionController;
        if (mc != null)
        {
            // 서버 최종 위치를 보정 참조값으로만 전달
            // navPath가 이미 완주했거나 다음 SetNavPath() 전까지 위치 기준점으로 활용
            Vector3 serverPos = new Vector3(
                endMovePkt.ServerPosInfo.X,
                endMovePkt.ServerPosInfo.Y,
                endMovePkt.ServerPosInfo.Z);

            mc.SetServerRefPos(serverPos, endMovePkt.ServerTime);

            Debug.Log($"[S_MOVE_END] Minion {targetId} " +
                      $"serverFinalPos=({serverPos.x:F2},{serverPos.z:F2})");
            return; // ← navPath 이동 및 상태에 일절 간섭하지 않음
        }

        // 플레이어 처리 (기존 로직 그대로)
        Vector3 clientPosBefore = bc.transform.position;

        PosInfo finalPos = endMovePkt.ServerPosInfo;
        Vector3 playerServerPos = new Vector3(finalPos.X, finalPos.Y, finalPos.Z);

        float diff = Vector3.Distance(clientPosBefore, playerServerPos);
        if (diff > 0.05f)
            Debug.LogWarning($"[S_MOVE_END] desync: diff={diff:F2}, client={clientPosBefore}, server={playerServerPos}");

        bc.PosInfo = finalPos;
        bc._isMoving = false;
        bc.transform.position = playerServerPos;
        bc.State = endMovePkt.ServerPosInfo.State;
        Debug.Log(endMovePkt.ServerPosInfo.State);
    }

    internal static void S_MOVE_STARTHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }

    internal static void S_MINION_MOVEHandler(PacketSession session, IMessage message)
    {
        S_MINION_MOVE minionMovePkt = message as S_MINION_MOVE;
        if (minionMovePkt == null)
        {
            Debug.LogError("[S_MINION_MOVEHandler] message casting failed");
            return;
        }

        var objectService = Bootstrapper.Instance.ObjectService;
        int targetId = minionMovePkt.ObjectId;

        GameObject go = objectService.FindById(targetId);
        if (go == null)
        {
            Debug.LogWarning($"[S_MINION_MOVEHandler] objectId={targetId} not found");
            return;
        }

        MinionController mc = go.GetComponent<MinionController>();
        if (mc == null)
        {
            Debug.LogWarning($"[S_MINION_MOVEHandler] objectId={targetId} has no MinionController");
            return;
        }

        // PosInfo 리스트 → Vector3 리스트 변환
        List<Vector3> navPath = new List<Vector3>(minionMovePkt.NavPath.Count);
        foreach (PosInfo pt in minionMovePkt.NavPath)
        {
            navPath.Add(new Vector3(pt.X, pt.Y, pt.Z));
        }

        if (navPath.Count == 0)
        {
            Debug.LogWarning($"[S_MINION_MOVEHandler] objectId={targetId} empty navPath received");
            return;
        }

        //Debug.Log($"[S_MINION_MOVEHandler] objectId={targetId} navPath.Count={navPath.Count}" +
        //          $" first=({navPath[0].x:F2},{navPath[0].z:F2})" +
        //          $" last=({navPath[navPath.Count - 1].x:F2},{navPath[navPath.Count - 1].z:F2})");

        // MinionController에 경로 전달 → 내부에서 Update마다 따라 이동
        mc.SetMoveSpeed(minionMovePkt.Speed);
        mc.SetNavPath(navPath);
    }

    internal static void S_ATTACKHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }

    public static void S_DIEHandler(PacketSession session, IMessage message)
    {
        S_DIE diePkt = message as S_DIE;
        IObjectService objectService = Bootstrapper.Instance.ObjectService;

        // 내 플레이어가 죽은 경우 — 제거하지 않고 사망 상태만 처리
        if (objectService.MyPlayer != null && objectService.MyPlayer.Id == diePkt.TargetId)
        {
            objectService.MyPlayer.State = Google.Protobuf.Enum.MoveState.Die;
            Debug.Log("S_DIE MyPlayer");
            // TODO: 리스폰 처리
            return;
        }

        GameObject go = objectService.FindById(diePkt.TargetId);
        if (go == null) 
            return;

        objectService.Remove(diePkt.TargetId);
    }

    internal static void S_START_GAMEHandler(PacketSession session, IMessage message)
    {
        throw new NotImplementedException();
    }

    internal static void S_HP_CHANGEHandler(PacketSession session, IMessage message)
    {
        S_HP_CHANGE pkt = message as S_HP_CHANGE;
        Debug.Log($"[S_HP_CHANGE] targetId={pkt.TargetId} hp={pkt.CurrentHp}/{pkt.MaxHp}");

        GameObject go = Bootstrapper.Instance.ObjectService.FindById(pkt.TargetId);
        if (go == null) 
            return;
        go.GetComponent<BaseController>()?.SetHp(pkt.CurrentHp, pkt.MaxHp);
    }

    internal static void S_END_GAMEHandler(PacketSession session, IMessage message)
    {
        Debug.Log("[S_END_GAME] Game Over");
    // TODO : 게임 종료 UI 표시
    // Managers.UI.ShowPopupUI<UI_GameResult>();
    }

    internal static void S_RESPAWNHandler(PacketSession session, IMessage message)
    {
        S_RESPAWN pkt = message as S_RESPAWN;

        IObjectService objectService = Bootstrapper.Instance.ObjectService;

        GameObject go = objectService.FindById(pkt.PlayerId);
        if (go == null) return;

        BaseController bc = go.GetComponent<BaseController>();
        Vector3 pos = new Vector3(pkt.X, pkt.Y, pkt.Z);
        bc.transform.position = pos;
        bc.SetHp(pkt.CurrentHp, pkt.MaxHp);
        bc.State = MoveState.Idle;
    }
}
