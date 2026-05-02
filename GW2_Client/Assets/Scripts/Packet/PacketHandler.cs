using Google.Protobuf;
using Google.Protobuf.Enum;
using Google.Protobuf.Protocol;
using Google.Protobuf.Struct;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PacketHandler
{
    public static void S_ENTER_GAMEHandler(PacketSession session, IMessage message)
    {
        S_ENTER_GAME enterGamePkt = message as S_ENTER_GAME;
        Debug.Log(enterGamePkt.Player.ObjectType);
        IObjectService objectService = Bootstrapper.Instance.ObjectService;

        objectService.Add(enterGamePkt.Player, true);
        //IUIService uIService = Bootstrapper.Instance.UIService;
        //uIService.ClosePopupUI();
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
        Debug.Log($"S_LOGIN : {networkService.GetNetworkId()}");
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

        Debug.Log($"[S_MOVE RECV] ObjectId={movePkt.ObjectId} MyId={objectService.MyPlayer?.Id}"); // ← 진입 즉시

        int targetId = movePkt.ObjectId;
        GameObject go = objectService.FindById(targetId);
        if (go == null)
        {
            Debug.Log($"[S_MOVEHandler] objectService findById is null. id={targetId}");
            return;
        }

        // 내 플레이어는 처리하지 않음
        if (objectService.MyPlayer != null && objectService.MyPlayer.Id == movePkt.ObjectId)
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
        pos.Yaw = movePkt.ServerPosInfo.Yaw;
        //pos.State = movePkt.ServerPosInfo.State;
        pos.State = MoveState.Run;

        bc._isMoving = true;
        bc.PosInfo = pos;
        bc.LastServerTime = movePkt.ServerTime;
        //bc.State = movePkt.ServerPosInfo.State;
        Debug.Log($"[S_MOVEHandler] Player {targetId} " +
                  $"serverPos=({pos.X:F2},{pos.Y:F2},{pos.Z:F2}) state={movePkt.ServerPosInfo.State}");
    }

    public static void S_SKILLHandler(PacketSession session, IMessage message)
    {
        S_SKILL skillPkt = message as S_SKILL;
        IObjectService objectService = Bootstrapper.Instance.ObjectService;

        GameObject attacker = objectService.FindById((int)skillPkt.AttackerId);
        GameObject target = objectService.FindById((int)skillPkt.TargetId);

        if (attacker == null) 
            return;

        // 터렛 공격
        TurretController tc = attacker.GetComponent<TurretController>();
        if (tc != null) 
        { 
            tc.OnAttack((int)skillPkt.TargetId); 
            return; 
        }

        BaseController attackerBc = attacker.GetComponent<BaseController>();
        if (attackerBc == null) 
            return;

        attackerBc.State = Google.Protobuf.Enum.MoveState.Skill;

        if (objectService.MyPlayer != null &&
            objectService.MyPlayer.Id == (int)skillPkt.AttackerId)
            return;

        int skillId = skillPkt.SkillId;
        Vector3 effectPos = attacker.transform.position;
        Vector3 dir = (target != null)
            ? (target.transform.position - effectPos).normalized
            : (skillPkt.DirX != 0f || skillPkt.DirZ != 0f)
                ? new Vector3(skillPkt.DirX, 0f, skillPkt.DirZ).normalized
                : attacker.transform.forward;

        if (skillId == 1)
        {
            attackerBc.PlayAttackEffect(target);
            attackerBc.SetAttackAnim(true);
            attackerBc.StartCoroutine(attackerBc.ResetAttackAnim(1.0f));
        }
        else
        {
            Vector3 worldPos = (target != null)
                ? target.transform.position
                : new Vector3(skillPkt.PosX, 0f, skillPkt.PosZ);

            attackerBc.PlaySkillEffect(skillId, effectPos, worldPos, dir);
            attackerBc.SetSkillAnim(true);
            attackerBc.StartCoroutine(attackerBc.ResetSkillAnim(1.5f));
        }
    }


    public static void S_SPAWNHandler(PacketSession session, IMessage message)
    {
        S_SPAWN spawnPacket = message as S_SPAWN;
        Debug.Log("spawn");
        IObjectService objectService = Bootstrapper.Instance.ObjectService;
        foreach (ObjectInfo obj in spawnPacket.Players)
        {
            // 이미 존재하는 오브젝트 = 리스폰 케이스 (Die 상태에서 재등장)
            GameObject existing = objectService.FindById(obj.ObjectId);
            if (existing != null)
            {
                BaseController bc = existing.GetComponent<BaseController>();
                if (bc != null)
                {
                    Vector3 respawnPos = new Vector3(obj.PosInfo.X, obj.PosInfo.Y, obj.PosInfo.Z);
                    existing.transform.position = respawnPos;
                    bc._isMoving = false;
                    bc.State = MoveState.Idle;
                }
                continue;
            }
            objectService.Add(obj, myPlayer: false);
        }
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
        //networkService.SetRoomId(lobbyPkt.RoomId);
        
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
        if (objectService.MyPlayer != null && objectService.MyPlayer.Id == targetId)
            return;

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

            //Debug.Log($"[S_MOVE_END] Minion {targetId} " + $"serverFinalPos=({serverPos.x:F2},{serverPos.z:F2})");
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
        bc.transform.position = new Vector3(playerServerPos.x, bc.transform.position.y, playerServerPos.z);
        bc.State = endMovePkt.ServerPosInfo.State;
        //Debug.Log(endMovePkt.ServerPosInfo.State);
        Debug.Log($"[S_MOVE_END] Player {targetId} snap to ({playerServerPos.x:F2},{playerServerPos.y:F2},{playerServerPos.z:F2}) from ({clientPosBefore.x:F2},{clientPosBefore.y:F2},{clientPosBefore.z:F2})");
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

        // 내 플레이어가 죽은 경우
        if (objectService.MyPlayer != null && objectService.MyPlayer.Id == diePkt.TargetId)
        {
            objectService.MyPlayer.SetBodyActive(false);
            objectService.MyPlayer.SetAlive(false);   // IsAlive=false + 메시 숨기기
            objectService.MyPlayer.ClearMovement();
            UI_KDA.OnKdaUpdate?.Invoke(0, 1, 0);
            return;
        }

        // 내 플레이어가 킬한 경우
        if (objectService.MyPlayer != null && objectService.MyPlayer.Id == diePkt.AttackerId)
            UI_KDA.OnKdaUpdate?.Invoke(1, 0, 0);

        // Remote 오브젝트 처리
        GameObject go = objectService.FindById(diePkt.TargetId);
        if (go == null) return;

        if (go.GetComponent<PlayerController>() != null)
        {
            // 플레이어: 리스폰 있으므로 오브젝트 유지, 메시만 숨기기
            go.GetComponent<BaseController>().SetAlive(false);
        }
        else
        {
            // 미니언 / 포탑 / 넥서스 등 리스폰 없는 오브젝트 제거
            objectService.Remove(diePkt.TargetId);
        }
    }


    internal static void S_START_GAMEHandler(PacketSession session, IMessage message)
    {
        Debug.Log("[S_START_GAME] All players joined. Game starting.");
    }

    internal static void S_HP_CHANGEHandler(PacketSession session, IMessage message)
    {
        S_HP_CHANGE pkt = message as S_HP_CHANGE;
        //Debug.Log($"[S_HP_CHANGE] targetId={pkt.TargetId} hp={pkt.CurrentHp}/{pkt.MaxHp}");

        GameObject go = Bootstrapper.Instance.ObjectService.FindById(pkt.TargetId);
        if (go == null) 
            return;
        go.GetComponent<BaseController>()?.SetHp(pkt.CurrentHp, pkt.MaxHp);
    }

    internal static void S_END_GAMEHandler(PacketSession session, IMessage message)
    {
        S_END_GAME pkt = message as S_END_GAME;
        Debug.Log("[S_END_GAME] Game Over");
        // TODO : 게임 종료 UI 표시

        IUIService uiService = Bootstrapper.Instance.UIService;
        uiService.ShowPopupUI<UI_GameResult>();
    }

    internal static void S_RESPAWNHandler(PacketSession session, IMessage message)
    {
        S_RESPAWN pkt = message as S_RESPAWN;
        IObjectService objectService = Bootstrapper.Instance.ObjectService;

        GameObject go = objectService.FindById(pkt.PlayerId);
        if (go == null) return;

        BaseController bc = go.GetComponent<BaseController>();

        // 스폰 위치로 이동
        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(bc.SpawnPosition);
            agent.ResetPath();  // ← 추가
        }
        else
            bc.transform.position = bc.SpawnPosition;

        // 복구
        bc.SetAlive(true);
        bc.SetHp(pkt.CurrentHp, pkt.MaxHp);

        // MyPlayer 입력 복원
        MyPlayerController myPlayer = go.GetComponent<MyPlayerController>();
        if (myPlayer != null)
            myPlayer.RestoreInput();
    }

    internal static void S_HAND_SYNCHandler(PacketSession session, IMessage message)
    {
        S_HAND_SYNC pkt = message as S_HAND_SYNC;
        List<int> cardIds = new List<int>(pkt.CardIds);
        Debug.Log($"[S_HAND_SYNC] playerId={pkt.PlayerId} cards={string.Join(",", cardIds)}");

        // MyPlayerController에 저장 (UI가 아직 없어도 보존됨)
        MyPlayerController.SetPendingHandSync(cardIds);

        // UI가 이미 존재하면 즉시 반영 (재접속, 재진입 등)
        UI_CardPanel.OnHandSync?.Invoke(cardIds);
    }

    internal static void S_DRAW_CARDHandler(PacketSession session, IMessage message)
    {
        S_DRAW_CARD pkt = message as S_DRAW_CARD;
        Debug.Log($"[S_DRAW_CARD] playerId={pkt.PlayerId} cardId={pkt.CardId}");
        UI_CardPanel.OnDrawCard?.Invoke(pkt.CardId);
    }

    internal static void S_GOLD_UPDATEHandler(PacketSession session, IMessage message)
    {
        S_GOLD_UPDATE pkt = message as S_GOLD_UPDATE;
        //Debug.Log($"[S_GOLD_UPDATE] gold={pkt.Gold}");
        UI_Store.OnGoldUpdate?.Invoke(pkt.Gold);
    }

    internal static void S_BUY_RESULTHandler(PacketSession session, IMessage message)
    {
        S_BUY_RESULT pkt = message as S_BUY_RESULT;
        Debug.Log($"[S_BUY_RESULT] success={pkt.Success} cardId={pkt.CardId} gold={pkt.Gold}");
        UI_Store.OnBuyResult?.Invoke(pkt.Success, pkt.CardId, pkt.Gold);
    }

    internal static void S_CHARACTER_SELECTEDHandler(PacketSession session, IMessage message)
    {
        S_CHARACTER_SELECTED pkt = message as S_CHARACTER_SELECTED;
        UI_Select.OnCharSelected?.Invoke(pkt.PlayerId, pkt.PlayerType, pkt.IsCancel);
    }

    internal static void S_BUFF_APPLIEDHandler(PacketSession session, IMessage message)
    {
        S_BUFF_APPLIED pkt = message as S_BUFF_APPLIED;
        Debug.Log($"[S_BUFF_APPLIED] target={pkt.TargetId} type={pkt.BuffType} value={pkt.Value} duration={pkt.Duration}");

        if (pkt.BuffType == BuffType.BuffSpeed)
        {
            IObjectService objectService = Bootstrapper.Instance.ObjectService;
            GameObject go = objectService.FindById(pkt.TargetId);
            if (go != null)
                go.GetComponent<BaseController>()?.ApplySpeedBuff(pkt.Value, pkt.Duration);
        }

        if (pkt.BuffType == BuffType.BuffAttackSpeed)
        {
            IObjectService objectService = Bootstrapper.Instance.ObjectService;
            GameObject go = objectService.FindById(pkt.TargetId);
            go?.GetComponent<BaseController>()?.ApplyAttackSpeedBuff(pkt.Value, pkt.Duration);

            if (objectService.MyPlayer != null && objectService.MyPlayer.Id == (int)pkt.TargetId)
                MyPlayerController.OnAttackSpeedBuffed?.Invoke(pkt.Value);  // ← 여기로 이동
        }


    }

    internal static void S_STUNHandler(PacketSession session, IMessage message)
    {
        S_STUN pkt = message as S_STUN;
        GameObject go = Bootstrapper.Instance.ObjectService.FindById(pkt.TargetId);
        if (go == null) return;
        go.GetComponent<BaseController>()?.ApplyStun(pkt.Duration);
    }

    internal static void S_DESPAWNHandler(PacketSession session, IMessage message)
    {
        S_DESPAWN pkt = message as S_DESPAWN;
        if (pkt == null) return;
        IObjectService objectService = Bootstrapper.Instance.ObjectService;
        objectService.Remove(pkt.TargetId);
    }

    internal static void S_FIND_GAMEHandler(PacketSession session, IMessage message)
    {
        S_FIND_GAME pkt = message as S_FIND_GAME;
        Debug.Log($"S_FIND_GAMEHandler roomId={pkt.RoomId}");

        INetworkService network = Bootstrapper.Instance.NetworkService;
        network.SetRoomId(pkt.RoomId);

        // 받은 roomId로 C_ENTER_GAME 즉시 전송
        C_ENTER_GAME enterPkt = new C_ENTER_GAME();
        enterPkt.RoomId = pkt.RoomId;
        enterPkt.PlayerIndex = network.GetNetworkId();
        enterPkt.GameMode = network.GetGameMode();  // ← 아래에서 저장한 값 사용
        network.Send(enterPkt);

        Bootstrapper.Instance.SceneService.LoadScene(Define.Scene.GameScene);
    }
}
