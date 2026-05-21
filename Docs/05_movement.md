# 05. 이동 시스템 (심화)

## 1. 개요 — 서버 권위 이동 모델

GW2의 이동은 **서버 권위(Server-Authoritative)** 방식입니다.

```
클라이언트                          서버
   │                                  │
   ├─ 클릭 → C_MOVE 전송 ────────────►│
   │                                  ├─ A* 경로탐색
   │                                  ├─ 경로 저장 (Player._path)
   │◄──── S_MOVE (100ms 주기) ────────┤
   │◄──── S_MOVE_END (도착 시) ───────┤
   │                                  │
   (클라이언트는 NavMesh로 예측 이동,
    서버 확인 시 보정)
```

| 대상 | 이동 방식 |
|------|-----------|
| MyPlayer | 클라이언트 예측 + 서버 보정 |
| 타 플레이어 | S_MOVE 위치를 직접 적용 |
| 미니언 | S_MINION_MOVE로 전체 경로 수신, 클라이언트가 경로 추적 |

---

## 2. 서버 플레이어 이동 처리

### 2-1. C_MOVE 수신 → A* 경로탐색

**파일**: `Room.cpp::HandleMovePlayer()`

```
C_MOVE 수신
    │
    ├─ ValidateMove()        ← 이동 유효성 검증 (walkable 여부)
    ├─ WorldToGrid() × 2    ← 출발·목적지를 그리드 좌표로 변환
    ├─ FindPath()            ← A* 경로탐색 (allowedLaneId=0 → 전체 허용)
    ├─ SmoothPath()          ← 경로 스무딩 (불필요한 꺾임 제거)
    └─ HandleMovePlayerInternal()
           ├─ player->RequestMove(worldPath)  ← 경로 저장
           └─ BroadcastMoving()               ← S_MOVE 브로드캐스트
```

### 2-2. 플레이어 이동 Update

**파일**: `Player.cpp:58::UpdateController()`

```cpp
// 매 틱(30Hz) 실행
float remainMoveDist = _moveSpeed * _speedMult * deltaTime;

while (remainMoveDist > 0.0f && _pathIndex < _path.size())
{
    Vector3 target = _path[_pathIndex];
    float dist = (target - _posVector).Length();

    if (dist <= remainMoveDist)
    {
        _posVector = target;   // 웨이포인트 도달
        ++_pathIndex;
        continue;
    }

    // 부분 이동
    _posVector += dir.Normalized() * remainMoveDist;
    return;
}

// 경로 완주 → Idle
if (_pathIndex >= _path.size())
    SetMoveState(MOVE_STATE_IDLE);
```

### 2-3. S_MOVE 브로드캐스트 조건

**파일**: `Object.h:7`, `Room.cpp::BroadcastMoving()`

```cpp
constexpr float MOVE_BROADCAST_INTERVAL = 0.1f;  // 100ms = 10Hz

// ShouldBroadcastMove() 조건
// ① 100ms 경과, 또는 ② MarkForceBroadcastMove() 호출 시 즉시
```

### 2-4. S_MOVE_END — 이동 완료 판정

**파일**: `Room.cpp::BroadcastMovingEnd()`

```
경로 완주 (_pathIndex >= _path.size())
    │
    └─ BroadcastMovingEnd()
           └─ S_MOVE_END 브로드캐스트
                  ├─ object_id
                  ├─ server_pos_info (최종 위치)
                  └─ server_time
```

---

## 3. 미니언 이동

### 3-1. 레인 경로탐색

**파일**: `Room.cpp::HandleMinionMove()` / `NavigationSystem.cpp::FindPath()`

미니언은 레인(Lane)을 따라 이동합니다. A* 탐색 시 `allowedLaneId`로 특정 레인 셀만 허용합니다.

```cpp
// NavigationSystem::FindPath() 시그니처
bool FindPath(
    const WalkableGrid& grid,
    int32 startX, int32 startZ,
    int32 endX,   int32 endZ,
    vector<GridCell*>& outPath,
    uint8 allowedLaneId     // 0이면 모든 walkable 허용
);
```

| allowedLaneId | 효과 |
|---------------|------|
| 0 | 레인 필터 해제 — 모든 walkable 셀 허용 |
| 1 | 상단 레인만 허용 |
| 3 | 하단 레인만 허용 |

### 3-2. S_MINION_MOVE — 전체 경로 전송 방식

**파일**: `Room.cpp::HandleMinionMove()`

```
A* 경로탐색 완료
    │
    ├─ minion->RequestMove(worldPath)  ← 서버 미니언 경로 저장
    └─ S_MINION_MOVE 브로드캐스트
           ├─ object_id
           ├─ nav_path[]   ← 전체 경로 웨이포인트
           └─ speed
```

플레이어의 S_MOVE(100ms 위치 동기화)와 달리, 미니언은 경로 전체를 한 번에 전송합니다.  
클라이언트는 받은 경로를 따라 매 프레임 이동하므로 추가 동기화가 불필요합니다.

### 3-3. 미니언 상태머신

**파일**: `Minion.cpp::UpdateController()`

```
MINION_IDLE
    │
    ├─ 주변 적 탐지 → MINION_CHASE_TARGET
    └─ 남은 waypoint 있음 → MINION_LINE_TRACE

MINION_LINE_TRACE  (레인 추종)
    │
    ├─ 적 탐지 → MINION_CHASE_TARGET
    ├─ 현재 waypoint 도달 → 다음 waypoint로
    └─ 모든 waypoint 완료 → MINION_IDLE
    │
    경로 요청 조건:
    ├─ _pathPending == false (이전 DoAsync 완료)
    ├─ _path 비어있거나 목표 변경
    └─ _repathCoolDown <= 0 (0.5초 간격)
    → room->DoAsync(&Room::HandleMinionMove, ...)

MINION_CHASE_TARGET  (타겟 추적)
    │
    ├─ 타겟 사망 → MINION_LINE_TRACE
    ├─ 공격 범위 내 → MINION_ATTACK
    ├─ leash 이탈 (detectionRange × 1.5) → MINION_LINE_TRACE
    └─ 경로 요청 (0.2초 간격)
    → room->DoAsync(&Room::HandleChaseMove, ...)

MINION_ATTACK  (공격)
    │
    ├─ 타겟 사망 → MINION_LINE_TRACE
    ├─ 사거리 이탈 (attackRange × 1.2) → MINION_CHASE_TARGET
    └─ 쿨다운 완료 → room->DoAsync(&Room::HandleMinionAttack, ...)
```

### 3-4. 타겟 우선순위

**파일**: `Minion.cpp:480::GetTargetPriority()`

| 오브젝트 타입 | 우선순위 점수 |
|---------------|---------------|
| Player | 1 |
| Minion | 2 |
| Turret | 3 |
| Nexus | 1 |
| 아군 | -1 (제외) |

> 현재 Nexus 우선순위가 Player와 동일(1점)입니다. 의도된 설계인지 확인 필요.

### 3-5. _pathPending 가드

미니언은 `DoAsync`로 Room 스레드에 경로 요청을 보냅니다. 응답 전에 중복 요청을 방지하기 위해 `_pathPending` 플래그를 사용합니다.

```cpp
// UpdateLaneTrace에서 요청 전
_pathPending = true;
room->DoAsync(&Room::HandleMinionMove, minionSelf, nowWp, ...);

// HandleMinionMove 완료 후 (RAII PendingGuard로 자동 해제)
// HandleChaseMove는 수동 ClearPathPending() 호출 필요
```

---

## 4. 클라이언트 이동 예측

### 4-1. NavMesh 클라이언트 예측

**파일**: `MyPlayerController.cs::StartMovePrediction()`

```csharp
// C_MOVE 전송과 동시에 클라이언트에서도 NavMesh 경로 예측
NavMeshPath navPath = new NavMeshPath();
NavMesh.CalculatePath(transform.position, destination, AllAreas, navPath);
_path = new List<Vector3>(navPath.corners);
_pathIndex = 0;
```

서버 응답(S_MOVE)을 기다리지 않고 즉시 이동을 시작합니다.

### 4-2. NavMeshAgent.updatePosition = false 구조

**파일**: `MyPlayerController.cs::Init()`

```csharp
_navAgent.updatePosition = false;  // NavMeshAgent가 Transform을 직접 제어하지 않음
_navAgent.updateRotation = false;
```

`updatePosition = false`로 설정하면:
- NavMeshAgent는 경로 계산(`NavMesh.CalculatePath`)에만 사용
- 실제 이동은 `UpdateMoving()`에서 `transform.position`을 수동으로 갱신

```csharp
// UpdateMoving() — 매 프레임
Vector3 direction = (waypoint - transform.position).normalized;
transform.position += direction * _moveSpeed * Time.deltaTime;
```

### 4-3. S_MOVE_END 수신 시 보정

**파일**: `PacketHandler.cs::S_MOVE_ENDHandler()`

```csharp
if (objectService.MyPlayer.Id == targetId)
{
    Vector3 serverPos = new Vector3(endMovePkt.ServerPosInfo.X, y, endMovePkt.ServerPosInfo.Z);
    float differ = Vector3.Distance(myPlayer.transform.position, serverPos);

    if (differ > 3.0f)
    {
        // 3m 이상 차이 = 텔레포트 또는 심각한 desync → 강제 보정
        NavMeshAgent agent = myPlayer.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.Warp(serverPos);   // NavMesh 위에 올바르게 착지
        else
            myPlayer.transform.position = serverPos;
        myPlayer.ClearMovement();
    }
    return;
}
```

| 차이 | 처리 |
|------|------|
| < 3m | 예측이 정확함 — 무시 (예측 완료로 간주) |
| ≥ 3m | 텔레포트 또는 desync — 서버 위치 강제 적용 |

---

## 5. 텔레포트 처리

### 5-1. 서버 — Mobility 카드 조건

**파일**: `Room.cpp:347`

```cpp
if (cardStat.buffType == 0 && cardStat.damage == 0 && cardStat.heal == 0)
{
    GameMath::Vector3 dest;
    dest._x = skillPkt.pos_x();
    dest._z = skillPkt.pos_z();

    player->_path.clear();
    player->_pathIndex = 0;
    player->SetPosVector(dest);       // 즉시 위치 변경
    player->SetMoveState(IDLE);

    // S_MOVE_END로 클라이언트에 최종 위치 전달
    S_MOVE_END movePkt;
    pos->set_x(dest._x); pos->set_z(dest._z);
    Broadcast(S_MOVE_END);
    return true;
}
```

### 5-2. 클라이언트 — Warp 처리

텔레포트 거리(보통 8m 이하)는 3m 임계값을 초과하므로 `agent.Warp()`로 즉시 이동합니다.

```csharp
// PacketHandler.cs::S_MOVE_ENDHandler()
NavMeshAgent agent = myPlayer.GetComponent<NavMeshAgent>();
agent.Warp(serverPos);   // NavMesh 상의 가장 가까운 점으로 이동
myPlayer.ClearMovement();
```

`agent.Warp()`를 사용하는 이유: `transform.position`을 직접 수정하면 NavMesh 외부로 이동했을 때 이후 경로탐색에 문제가 생길 수 있습니다. `Warp()`는 NavMesh 위의 유효한 위치로 착지를 보장합니다.

---

## 6. WalkableGrid — NavMesh 데이터 구조

**파일**: `NavigationSystem.h:43`

```
WalkableGrid
├── width, height   : 그리드 크기
├── cellSize        : 셀 크기 (월드 단위)
├── origin          : 그리드 원점 (월드 좌표)
└── cells[]         : GridCell 배열 (width × height)

GridCell
├── walkable        : 이동 가능 여부
├── height          : 지형 높이
├── neighbors[4]    : N/E/S/W 이웃 셀 연결 여부
├── x, z            : 그리드 인덱스
└── laneId          : 소속 레인 (0=레인 없음)
```

그리드 파일은 Unity NavMesh를 베이크한 결과물에서 Export합니다:
- `navgrid.bin` : WalkableGrid 바이너리
- `laneMap.bin` : 레인 ID 정보
- `laneRoutes.json` : 미니언 경유 웨이포인트

---

## 7. A* 알고리즘

**파일**: `NavigationSystem.cpp::FindPath()`

```
휴리스틱: 맨해튼 거리
    h = |x1-x2| + |z1-z2|

탐색 조건:
    ① cell.walkable == true
    ② allowedLaneId == 0  OR  cell.laneId == allowedLaneId

자료구조:
    - openList: priority_queue<OpenNode> (f값 오름차순)
    - nodeRecords: unordered_map<NodeKey, NodeRecord>

결과:
    - outPath: vector<GridCell*> (그리드 좌표)
    - 호출부에서 GridToWorld()로 월드 좌표 변환
```

---

## 8. 알려진 한계 및 개선 여지

| 항목 | 현황 | 개선 방향 |
|------|------|-----------|
| MyPlayer S_MOVE_END 보정 임계값 | 3.0m 하드코딩 | 이동속도 × RTT 기반 동적 계산 |
| 미니언 ClearPathPending | HandleChaseMove에서 수동 호출 필요 | RAII 가드로 통일 |
| 클라이언트 예측 경로 vs 서버 경로 불일치 | desync 발생 시 3m 이상 차이 가능 | 서버 경로를 클라이언트에도 전달하는 방식 검토 |
| Nexus 타겟 우선순위 | Player와 동일 (1점) | Nexus를 최우선(4점)으로 변경 고려 |
| S_MOVE 브로드캐스트 주기 | 100ms (10Hz) | 이동속도에 따른 가변 주기 검토 |
