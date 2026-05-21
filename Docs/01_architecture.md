# 01. 서버 전체 아키텍처

## 개요

GW2 서버는 **IOCP 기반 C++ 게임 서버**로, 서버 권위(Server-Authoritative) 구조를 따릅니다.  
모든 게임 로직(이동, 전투, 버프, 스폰)은 서버에서 계산하고, 클라이언트는 결과를 받아 표현만 담당합니다.

---

## 디렉터리 구조

```
GW2_Server/
├── GW2_ServerCore/         # IOCP, 세션, 패킷 송수신 코어
├── GW2_CrossPlatformCore/  # 수학, 로그, 버퍼 유틸리티
└── GW2_Server/             # 게임 로직
    ├── Lobby.cpp/h         # 서버 진입점, Room 관리, NavMesh/Stats 로드
    ├── Room.cpp/h          # 게임 룸 (JobQueue 상속, 게임 루프)
    ├── Object.h/cpp        # 모든 게임 오브젝트 기반 클래스
    ├── Player.cpp/h        # 플레이어 오브젝트
    ├── Minion.cpp/h        # 미니언 오브젝트 (상태머신 포함)
    ├── Turret.cpp/h        # 포탑 오브젝트
    ├── Nexus.cpp/h         # 넥서스 오브젝트
    ├── Baron.cpp/h         # 바론 오브젝트
    ├── NavigationSystem.h/cpp  # A* 경로탐색, WalkableGrid
    ├── StatLoader.cpp/h    # Stats.json 파싱
    ├── CardManager.cpp/h   # 덱/핸드 관리
    └── Data/
        └── Stats.json      # 유닛 스탯 및 카드 정의
```

---

## 레이어 구조

```
[클라이언트 Unity]
       |   패킷 (Protobuf)
       ↓
[GW2_ServerCore — IOCP]
  └── GameSession: 패킷 수신 → ClientPacketHandler 디스패치
       |
       ↓
[Lobby — 싱글톤 (GLobby)]
  ├── NavMesh / LaneRoute 로드 (서버 시작 시 1회)
  ├── Stats.json 로드 (UnitStat, CardStat)
  ├── Room 생성·삭제 관리
  └── RunRooms(): 30Hz 게임 루프 (33ms tick)
       |
       ↓
[Room — JobQueue 상속]
  ├── _objects: unordered_map<id, ObjectRef>  (모든 오브젝트)
  ├── _players: unordered_map<id, PlayerRef>  (플레이어만)
  ├── UpdateRoom(deltaTime): 매 틱 호출
  └── Handler 함수들 (HandleSkill, HandleMovePlayer 등)
       |
       ↓
[Object 계층]
  Object (base)
  ├── Player
  ├── Minion
  ├── Turret
  ├── Nexus
  └── Baron
```

---

## 스레드 모델

| 스레드 | 역할 |
|--------|------|
| IOCP Worker Thread(s) | 네트워크 I/O, 패킷 수신·송신 |
| Lobby Main Thread | `RunRooms()` — 30Hz tick, `room->DoAsync()` 디스패치 |
| Room JobQueue | Room 전용 직렬 실행 큐 — 모든 Room 로직은 여기서만 실행 |

### 크로스 스레드 호출 규칙

Room 외부(미니언 스레드, IOCP 스레드)에서 Room 함수를 호출할 때는 반드시 `DoAsync`를 사용합니다.

```cpp
// Minion 스레드 → Room 스레드 (안전)
room->DoAsync(&Room::HandleMinionAttack, self, currentTarget);

// 직접 호출 (위험 — 절대 금지)
room->HandleMinionAttack(self, currentTarget);
```

`DoAsync`는 람다를 JobQueue에 enqueue하여 Room 스레드에서 직렬 실행을 보장합니다.

---

## Object 클래스 계층

**파일**: `GW2_Server/GW2_Server/Object.h`

```
Object (enable_shared_from_this<Object>)
│
├── 공통 데이터
│   ├── Protocol::ObjectInfo _objectInfo  ← Protobuf 메시지 (id, pos, stat, type)
│   ├── GameMath::Vector3 _posVector      ← 서버 실제 위치
│   ├── NavPath _path                     ← A* 결과 경로
│   └── size_t _pathIndex
│
├── 공통 메서드
│   ├── UpdateMovement(deltaTime)         ← 경로 추적 이동
│   ├── UpdateController(deltaTime)       ← 상태 갱신 (override)
│   ├── ApplyDamage(dmg) → bool          ← 사망 여부 반환
│   └── ShouldBroadcastMove()             ← 100ms 브로드캐스트 조건
│
└── 파생 클래스
    ├── Player   : 플레이어 이동/버프 타이머
    ├── Minion   : 상태머신 (IDLE→LINE_TRACE→CHASE→ATTACK)
    ├── Turret   : 고정 방어 오브젝트
    ├── Nexus    : 게임 종료 조건 오브젝트
    └── Baron    : 중립 몬스터
```

---

## Lobby 싱글톤

**파일**: `GW2_Server/GW2_Server/Lobby.cpp`

```cpp
LobbyRef GLobby = make_shared<Lobby>();  // 전역 싱글톤
```

### 초기화 순서 (`LobbyInit()`)

1. `navgrid.bin` 로드 → `WalkableGrid` 구성
2. `NavigationSystem::BuildConnections()` — 그리드 연결 구축
3. `laneMap.bin` 로드 — 레인 정보 주입
4. `laneRoutes.json` 로드 — 미니언 경유 웨이포인트
5. `Stats.json` 로드 — `UnitStat`, `CardStat` 파싱

### 게임 루프 (`RunRooms()`)

```cpp
// Lobby.cpp:218-253
const auto TICK_INTERVAL = chrono::milliseconds(33);  // 30Hz
while (_isRunning)
{
    float deltaTime = ...;
    LobbyUpdate(deltaTime);           // 각 Room에 DoAsync(&Room::UpdateRoom)
    sleep_for(TICK_INTERVAL - elapsed);
}
```

### Room 매칭

```cpp
// Lobby.cpp:156-174
RoomRef FindOrCreateRoom(int32 gameMode, int32 maxPlayers)
// 같은 gameMode + 빈 자리 있는 Room 우선 반환, 없으면 새 Room 생성
```

---

## 패킷 처리 흐름

```
클라이언트 전송
       ↓
GameSession::OnRecvPacket()          [GW2_ServerCore]
       ↓
ClientPacketHandler::HandlePacket()  [디스패치]
       ↓
Room::DoAsync(&Room::HandleXxx)      [Room JobQueue에 enqueue]
       ↓
Room::HandleXxx()                    [Room 스레드에서 직렬 실행]
       ↓
Room::Broadcast() / session->Send()  [결과 패킷 전송]
```

---

## 주요 상수

| 상수 | 값 | 위치 |
|------|----|------|
| `MOVE_BROADCAST_INTERVAL` | 0.1f (100ms) | `Object.h:7` |
| `TICK_INTERVAL` | 33ms (30Hz) | `Lobby.cpp:219` |
| `WAVE_MINION_COUNT` | 5 | `Room.h:121` |
| `MINION_SPAWN_COOLDOWN` | 30.0f초 | `Room.h:116` |
| `WAVE_SPAWN_INTERVAL` | 1.0f초 | `Room.h:122` |
| `GOLD_INCOME_INTERVAL` | 1.0f초 | `Room.h:136` |
| `GOLD_INCOME_AMOUNT` | 20 | `Room.h:137` |
| `MAX_MINION_COUNT` | 50 | `Room.h:124` |
