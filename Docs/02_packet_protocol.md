# 02. 패킷 프로토콜

## 개요

GW2는 **Google Protobuf**로 직렬화된 패킷을 사용합니다.  
패킷은 방향에 따라 `C_` (클라이언트→서버), `S_` (서버→클라이언트)로 구분됩니다.

- 서버 정의: `GW2_Server/GW2_Server/Protocol.pb.h`, `Struct.pb.h`
- 클라이언트 정의: `GW2_Client/Assets/Scripts/Packet/Protocol.cs`, `Struct.cs`
- 서버 핸들러: `GW2_Server/GW2_Server/ClientPacketHandler.cpp` (수신) / `Room.cpp` (처리)
- 클라이언트 핸들러: `GW2_Client/Assets/Scripts/Packet/PacketHandler.cs`

---

## 공통 메시지 타입

### StatInfo
**파일**: `Struct.pb.h:525` / `Struct.cs:862`

| 필드 | 타입 | 번호 | 설명 |
|------|------|------|------|
| `hp` | uint64 | 1 | 현재 HP |
| `max_hp` | uint64 | 2 | 최대 HP |
| `attack` | uint64 | 3 | 공격력 |
| `speed` | uint64 | 4 | 이동속도 |
| `attack_range` | float | 5 | 공격 사거리 |

> **주의**: `defense` 필드는 StatInfo에 없음. 방어력 버프는 `S_BUFF_APPLIED`로만 전달됩니다.

### PosInfo

| 필드 | 타입 | 설명 |
|------|------|------|
| `object_id` | int32 | 오브젝트 ID |
| `x`, `y`, `z` | float | 월드 좌표 |
| `yaw` | float | 회전값 |
| `state` | MoveState | IDLE / RUN / SKILL / DIE |

### ObjectInfo

| 필드 | 타입 | 설명 |
|------|------|------|
| `object_id` | int32 | 고유 ID |
| `room_id` | int32 | 소속 룸 |
| `object_type` | ObjectType | PLAYER / MINION / TURRET / NEXUS |
| `name` | string | 오브젝트 이름 |
| `pos_info` | PosInfo | 위치 정보 |
| `stat_info` | StatInfo | 스탯 정보 |
| `team_flag` | CampType | CAMP_HUMAN / CAMP_VILLAIN |

---

## 패킷 목록

### 클라이언트 → 서버

#### `C_MOVE`
플레이어 이동 요청.

| 필드 | 타입 | 설명 |
|------|------|------|
| `room_id` | int32 | 소속 룸 ID |
| `object_id` | int32 | 내 플레이어 ID |
| `start_pos` | PosInfo | 출발 위치 (클라이언트 현재 위치) |
| `target_pos` | PosInfo | 목적지 위치 |
| `client_time` | int32 | 클라이언트 타임스탬프 (ms) |

**서버 처리**: `Room::HandleMovePlayer()` → A* 경로탐색 → `_path` 저장

---

#### `C_SKILL`
공격 및 카드 스킬 사용 요청.

| 필드 | 타입 | 설명 |
|------|------|------|
| `room_id` | int32 | 소속 룸 ID |
| `attacker_id` | int32 | 시전자 ID |
| `target_id` | int32 | 대상 ID (없으면 0) |
| `command_id` | int32 | 1=기본공격, 그 외=카드 ID |
| `pos_x`, `pos_z` | float | 클릭 지점 좌표 |
| `dir_x`, `dir_z` | float | 스킬 방향 벡터 |
| `client_time` | int32 | 클라이언트 타임스탬프 (ms) |

**서버 처리**: `Room::HandleSkill()` — `target_id == 0`이면 논타겟 분기, 그 외 타겟 분기

---

#### `C_ENTER_GAME`
게임 룸 입장 요청.

| 필드 | 타입 | 설명 |
|------|------|------|
| `room_id` | int32 | 입장할 룸 ID |
| `player_index` | int32 | 플레이어 고유 ID |
| `game_mode` | int32 | 게임 모드 |

---

#### `C_ENTER_LOBBY`
로비 입장 요청 (로그인 성공 후 자동 전송).

#### `C_BUY_CARD`
카드 구매 요청.

| 필드 | 타입 | 설명 |
|------|------|------|
| `card_id` | int32 | 구매할 카드 ID |
| `player_id` | int32 | 플레이어 ID |

#### `C_SELECT_CHARACTER`
캐릭터 선택 요청.

| 필드 | 타입 | 설명 |
|------|------|------|
| `player_id` | int32 | 플레이어 ID |
| `player_type` | PlayerType | 선택 캐릭터 |
| `is_cancel` | bool | 선택 취소 여부 |

---

### 서버 → 클라이언트

#### `S_ENTER_GAME`
게임 입장 확인 — 내 플레이어 정보 전달.

| 필드 | 타입 | 설명 |
|------|------|------|
| `player` | ObjectInfo | 내 플레이어 전체 정보 (StatInfo 포함) |

**클라이언트 처리**: `PacketHandler.S_ENTER_GAMEHandler` → `ObjectService.Add(myPlayer:true)` → `SetPendingStatInfo()`

---

#### `S_SPAWN`
다른 오브젝트 등장 알림.

| 필드 | 타입 | 설명 |
|------|------|------|
| `players` | ObjectInfo[] | 등장하는 오브젝트 목록 |

**클라이언트 처리**: 이미 존재하는 ID면 리스폰 처리, 없으면 `ObjectService.Add()`

---

#### `S_MOVE`
오브젝트 이동 중 위치 동기화 (100ms마다).

| 필드 | 타입 | 설명 |
|------|------|------|
| `object_id` | int32 | 이동 중인 오브젝트 ID |
| `server_pos_info` | PosInfo | 서버 현재 위치 |
| `server_time` | float | 서버 타임스탬프 |

**클라이언트 처리**: MyPlayer는 무시. 미니언은 `SetServerRefPos()`만 갱신 (NavPath 이동 유지). 타 플레이어는 위치 직접 적용.

---

#### `S_MOVE_END`
이동 완료 및 최종 위치 확정.

| 필드 | 타입 | 설명 |
|------|------|------|
| `object_id` | int32 | 오브젝트 ID |
| `server_pos_info` | PosInfo | 서버 최종 위치 |
| `server_time` | float | 서버 타임스탬프 |

**클라이언트 처리**: MyPlayer는 3m 이상 차이 날 때만 강제 적용(텔레포트). 그 외는 스킵(예측 완료). 미니언은 참조 위치만 갱신.

---

#### `S_MINION_MOVE`
미니언 전체 NavPath 전달.

| 필드 | 타입 | 설명 |
|------|------|------|
| `object_id` | int32 | 미니언 ID |
| `nav_path` | PosInfo[] | 전체 경로 웨이포인트 목록 |
| `speed` | float | 이동속도 |

**클라이언트 처리**: `MinionController.SetNavPath()` — 매 Update마다 경로 추적 이동

---

#### `S_SKILL`
스킬/공격 이펙트 브로드캐스트.

| 필드 | 타입 | 설명 |
|------|------|------|
| `skill_id` | int32 | 1=기본공격, 그 외=카드 ID |
| `attacker_id` | int32 | 시전자 ID |
| `target_id` | int32 | 대상 ID (없으면 0) |
| `pos_x`, `pos_z` | float | 이펙트 위치 |
| `dir_x`, `dir_z` | float | 이펙트 방향 |

---

#### `S_BUFF_APPLIED`
버프 적용 알림 (대상 플레이어에게만 전송).

| 필드 | 타입 | 설명 |
|------|------|------|
| `target_id` | int32 | 버프 대상 플레이어 ID |
| `buff_type` | BuffType | 버프 종류 (enum) |
| `value` | float | 버프 값 |
| `duration` | float | 지속 시간 (초) |

> `BuffDefense`의 `value`는 데미지 감소 비율 (예: 0.3 = 30% 감소).  
> UI 표시 시 `value * 100`으로 변환 필요.

---

#### `S_HP_CHANGE`
HP 변경 알림 (전체 브로드캐스트).

| 필드 | 타입 | 설명 |
|------|------|------|
| `target_id` | int32 | 대상 ID |
| `current_hp` | float | 현재 HP |
| `max_hp` | float | 최대 HP |

---

#### `S_DIE`
오브젝트 사망 알림.

| 필드 | 타입 | 설명 |
|------|------|------|
| `target_id` | int32 | 사망 오브젝트 ID |
| `attacker_id` | int32 | 킬한 오브젝트 ID |

---

#### `S_RESPAWN`
플레이어 리스폰.

| 필드 | 타입 | 설명 |
|------|------|------|
| `player_id` | int32 | 리스폰 플레이어 ID |
| `current_hp` | float | 리스폰 HP |
| `max_hp` | float | 최대 HP |

---

#### `S_HAND_SYNC`
핸드(패) 전체 동기화.

| 필드 | 타입 | 설명 |
|------|------|------|
| `player_id` | int32 | 플레이어 ID |
| `card_ids` | int32[] | 현재 핸드 카드 ID 목록 |

---

#### `S_DRAW_CARD`
카드 드로우 알림 (카드 1장 추가).

| 필드 | 타입 | 설명 |
|------|------|------|
| `player_id` | int32 | 플레이어 ID |
| `card_id` | int32 | 드로우한 카드 ID |

---

#### `S_GOLD_UPDATE`
골드 변경 알림.

| 필드 | 타입 | 설명 |
|------|------|------|
| `gold` | int64 | 현재 골드 |

---

#### `S_BUY_RESULT`
카드 구매 결과.

| 필드 | 타입 | 설명 |
|------|------|------|
| `success` | bool | 구매 성공 여부 |
| `card_id` | int32 | 구매한 카드 ID |
| `gold` | int64 | 구매 후 남은 골드 |

---

#### `S_STUN`
스턴 적용 알림.

| 필드 | 타입 | 설명 |
|------|------|------|
| `target_id` | int32 | 대상 ID |
| `duration` | float | 스턴 지속 시간 |

---

#### `S_DESPAWN`
오브젝트 제거 알림 (미니언, 포탑 등 리스폰 없는 오브젝트).

| 필드 | 타입 | 설명 |
|------|------|------|
| `target_id` | int32 | 제거 오브젝트 ID |

---

#### `S_START_GAME`
게임 시작 알림 (모든 플레이어 입장 완료 시).

#### `S_END_GAME`
게임 종료 알림.

| 필드 | 타입 | 설명 |
|------|------|------|
| `winner` | CampType | 승리 진영 |

#### `S_CHARACTER_SELECTED`
캐릭터 선택 결과 브로드캐스트.

| 필드 | 타입 | 설명 |
|------|------|------|
| `player_id` | int32 | 플레이어 ID |
| `player_type` | PlayerType | 선택된 캐릭터 |
| `is_cancel` | bool | 취소 여부 |

---

## Enum 정의

### BuffType
| 값 | 이름 | 설명 |
|----|------|------|
| 0 | BuffNone | 없음 |
| 1 | BuffAttack | 공격력 배율 (value > 1.0) |
| 2 | BuffDefense | 데미지 감소 비율 (0.0~1.0) |
| 3 | BuffSpeed | 이동속도 배율 (value > 1.0) |
| 4 | BuffAttackSpeed | 공격속도 배율 (value < 1.0 = 쿨타임 감소) |

### MoveState
| 값 | 이름 |
|----|------|
| 0 | None |
| 1 | Idle |
| 2 | Run |
| 3 | Skill |
| 4 | Die |

### ObjectType
| 값 | 이름 |
|----|------|
| 0 | None |
| 1 | Player |
| 2 | Minion |
| 3 | Turret |
| 4 | Nexus |

### CampType
| 값 | 이름 |
|----|------|
| 0 | None |
| 1 | Human |
| 2 | Villain |
