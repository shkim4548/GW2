# 06. 게임 세션 생명주기

## 개요

서버 기준 게임 세션의 전체 흐름입니다.

```
서버 시작 → Lobby 초기화
    ↓
클라이언트 접속 → 로그인
    ↓
로비 입장
    ↓
캐릭터 선택
    ↓
게임 룸 매칭 → 게임 시작
    ↓
[게임 루프]
  이동 / 전투 / 버프 / 미니언 웨이브
    ↓
사망 / 리스폰
    ↓
넥서스 파괴 → 게임 종료
```

---

## 1. 서버 시작 및 Lobby 초기화

**파일**: `Lobby.cpp::LobbyInit()`

```
서버 실행
    │
    ├─ navgrid.bin 로드          → WalkableGrid 구성
    ├─ BuildConnections()        → 셀 연결 정보 구축
    ├─ laneMap.bin 로드          → 레인 ID 주입
    ├─ laneRoutes.json 로드      → 미니언 waypoint 설정
    ├─ Stats.json 로드           → UnitStat, CardStat 파싱
    └─ RunRooms() 시작           → 30Hz 게임 루프 가동
```

---

## 2. 로그인

```
클라이언트: C_LOGIN 전송
    │
서버: S_LOGIN(success=true) 응답
    │
클라이언트: C_ENTER_LOBBY 자동 전송
```

---

## 3. 로비 입장 및 룸 매칭

**파일**: `Lobby.cpp::OnClientEnter()`, `FindOrCreateRoom()`

```
C_ENTER_LOBBY 수신
    │
    ├─ _lobbyPlayers에 등록
    └─ S_ENTER_LOBBY 응답
           ├─ player_id
           └─ room_infos[] (현재 룸 목록)

C_FIND_GAME 수신 (매칭 요청)
    │
    ├─ FindOrCreateRoom(gameMode, maxPlayers)
    │       ├─ 빈 자리 있는 기존 룸 반환
    │       └─ 없으면 새 룸 생성 + RoomInit()
    │
    └─ S_FIND_GAME 응답
           └─ room_id

C_ENTER_GAME 수신
    │
    ├─ Lobby::EnterRoom(roomId, playerId)
    └─ Room::Enter(player)
           ├─ _players에 등록
           ├─ _objects에 등록
           └─ HandleEnterPlayer()
                  ├─ SyncObjectsToPlayer()   ← 기존 오브젝트 정보 전송
                  ├─ S_ENTER_GAME 전송       ← 내 플레이어 정보 (StatInfo 포함)
                  ├─ S_HAND_SYNC 전송        ← 초기 핸드 동기화
                  ├─ S_GOLD_UPDATE 전송      ← 초기 골드 (500)
                  └─ S_SPAWN 브로드캐스트   ← 다른 플레이어에게 새 입장 알림
```

---

## 4. 캐릭터 선택

**파일**: `Room.cpp::HandleSelectCharacter()`, `HandleConfirmCharacter()`

```
C_SELECT_CHARACTER 수신
    │
    ├─ _pendingSelections에 기록
    └─ S_CHARACTER_SELECTED 브로드캐스트

모든 플레이어 선택 완료
    │
    └─ HandleConfirmCharacter()
           ├─ Player::InitPlayer()   ← Stats.json 기준 스탯 적용
           └─ StartGame() 예약
```

---

## 5. 게임 시작

**파일**: `Room.cpp::StartGame()`

```
StartGame()
    │
    ├─ _gameStarted = true
    ├─ S_START_GAME 브로드캐스트
    ├─ 포탑 스폰 (각 진영 레인별)
    ├─ 넥서스 스폰 (각 진영)
    └─ 바론 스폰
```

---

## 6. 게임 루프

**파일**: `Lobby.cpp::RunRooms()` → `Room::UpdateRoom(deltaTime)`

```
[30Hz Tick — 33ms 간격]
    │
    Room::UpdateRoom(deltaTime)
    │
    ├─ 각 Object::UpdateController(deltaTime)
    │       ├─ Player: 경로 이동, 버프 타이머 감소
    │       └─ Minion: 상태머신 (IDLE→LINE_TRACE→CHASE→ATTACK)
    │
    ├─ 이동 브로드캐스트 (100ms 주기)
    │       └─ ShouldBroadcastMove() → S_MOVE 브로드캐스트
    │
    ├─ 미니언 웨이브 스폰 (30초 간격)
    │       ├─ 레인 1(상단) + 레인 3(하단) 각 5마리씩
    │       └─ 1초 간격으로 순차 스폰
    │
    ├─ 리스폰 타이머 감소
    │       └─ 만료 시 HandleRespawnPlayer()
    │
    └─ 골드 자동 지급 (1초 간격, 20골드)
```

---

## 7. 전투

```
C_SKILL(commandId=1, targetId=적) 수신  ← 기본 공격
    │
    ├─ 공격 쿨다운 검증
    ├─ 대상 유효성 검증 (팀, 사망, 사거리)
    ├─ 데미지 계산
    │       damage = StatInfo.attack (기본 공격은 coeff 없음)
    │       damage × (1 - _defenseReduct)  ← 방어 버프 반영
    ├─ S_HP_CHANGE 브로드캐스트
    └─ 사망 시 → HandleRemoveObject()

C_SKILL(commandId=카드ID) 수신  ← 카드 스킬
    └─ HandleSkill() → 03_card_system.md 참조
```

---

## 8. 사망 처리

**파일**: `Room.cpp::HandleRemoveObject()`

```
HandleRemoveObject(targetId, attackerId)
    │
    ├─ 오브젝트 사망 처리 (SetIsDead)
    ├─ S_DIE 브로드캐스트
    │       ├─ target_id
    │       └─ attacker_id (킬 크레딧용)
    │
    ├─ [Player] 리스폰 타이머 등록 (_respawnTimers)
    │       └─ 기본 5초 후 HandleRespawnPlayer()
    │
    ├─ [Minion/Turret/Nexus] _objects에서 제거
    │       └─ S_DESPAWN 브로드캐스트
    │
    └─ [Nexus 사망] HandleNexusDead() → 게임 종료
```

---

## 9. 리스폰

**파일**: `Room.cpp::HandleRespawnPlayer()`

```
리스폰 타이머 만료
    │
    HandleRespawnPlayer(playerId)
    │
    ├─ FullHeal()                    ← HP 전체 회복
    ├─ 스폰 위치로 이동
    ├─ SetMoveState(IDLE)
    └─ S_RESPAWN 브로드캐스트
           ├─ player_id
           ├─ current_hp
           └─ max_hp
```

---

## 10. 게임 종료

**파일**: `Room.cpp::HandleNexusDead()`

```
넥서스 HP 0
    │
    HandleNexusDead(deadTeam)
    │
    ├─ _gameStarted = false
    ├─ 승리 진영 결정 (deadTeam의 반대 진영)
    └─ S_END_GAME 브로드캐스트
           └─ winner: CampType
```

---

## 타이밍 요약

| 이벤트 | 주기/조건 |
|--------|-----------|
| 게임 틱 | 33ms (30Hz) |
| S_MOVE 브로드캐스트 | 100ms (10Hz) |
| 미니언 웨이브 스폰 | 30초 간격 |
| 웨이브 내 스폰 간격 | 1초 |
| 골드 자동 지급 | 1초 간격, 20골드 |
| 플레이어 리스폰 | 사망 후 5초 |
| 미니언 타겟 탐색 | 0.1초 간격 |
| 미니언 레인 경로 재요청 | 0.5초 간격 |
| 미니언 추적 경로 재요청 | 0.2초 간격 |
