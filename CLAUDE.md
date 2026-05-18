# GW2 프로젝트 — Claude 작업 지침 (branch: window_server)

## 프로젝트 개요
- C++ 게임 서버 (IOCP, Protobuf) + Unity 클라이언트 구조의 RTS/MOBA
- 서버 권위(Server-Authoritative) 구조: 모든 게임 로직은 서버에서 처리
- 네트워킹: Protobuf 패킷, JobQueue 커맨드 패턴으로 Thread-safe 보장

## 핵심 경로
- 클라이언트: `D:\Dev\unity\GW2\GW2_Client\Assets\Scripts\`
- 서버: `D:\Dev\unity\GW2\GW2_Server\GW2_Server\`

## 아키텍처 요약

### 클라이언트
- **Controllers**: `BaseController` → `MyPlayerController` / `PlayerController` / `MinionController` / `TurretController` / `NexusController` / `BaronController`
- **Services**: `ObjectService`, `NetworkService`, `InputService` — `Bootstrapper` 싱글톤 경유 접근
- **패킷 흐름**: `PacketHandler` → `ObjectService.FindById()` → `GetComponent<Controller>()` → 상태 변경
- **버프 시스템**: 서버 `S_BUFF_APPLIED` 패킷 → `PacketHandler` → `BaseController.ApplyBuff()` → `MyPlayerController.OnBuffApplied(BuffType, float)` 이벤트 → `UI_StatusBox`
- **이동**: `NavMeshAgent.updatePosition = false`, 수동 이동 처리 (MyPlayer는 클라이언트 예측, Minion은 서버 경로 수신)

### 서버
- `DoAsync(&Room::Func, ...)` : 크로스 스레드 호출 시 반드시 사용
- `laneId=0` : A* FindPath에서 allowedLaneId=0이면 레인 필터 해제

## 작업 규칙 (필수 준수)
1. **코드 직접 편집 절대 금지** — 분석과 수정 제안만 한다. 실제 수정은 사용자가 직접 진행한다.
2. **현황 파악 = 코드 최신화** — 분석 전 반드시 관련 파일을 실제로 읽어 최신 상태를 확인한다. 이전 대화의 내용을 그대로 신뢰하지 않는다.
3. **제안 형식**: 파일명, 줄 번호, 변경 전/후 코드를 명시한다.
4. 서버 → 클라이언트 순서로 흐름을 추적한다.

## 주요 패킷 목록
| 패킷 | 방향 | 설명 |
|------|------|------|
| `S_ENTER_GAME` | 서버→클 | 내 플레이어 ObjectInfo(StatInfo 포함) 전달 |
| `S_SPAWN` | 서버→클 | 다른 오브젝트 등장 |
| `S_MOVE` | 서버→클 | 위치 동기화 |
| `S_MINION_MOVE` | 서버→클 | 미니언 전체 경로 전달 |
| `S_MOVE_END` | 서버→클 | 이동 완료 및 최종 위치 보정 |
| `S_SKILL` | 서버→클 | 스킬/공격 이펙트 |
| `S_BUFF_APPLIED` | 서버→클 | 버프 적용 (BuffType enum + value + duration) |
| `S_HP_CHANGE` | 서버→클 | HP 변경 |
| `S_DIE` | 서버→클 | 사망 처리 |
| `S_RESPAWN` | 서버→클 | 리스폰 |
| `C_SKILL` | 클→서버 | 스킬/공격 요청 |
| `C_MOVE` | 클→서버 | 이동 요청 |

## 버프 시스템 (BuffType enum)
| 값 | 이름 | 설명 |
|----|------|------|
| 0 | BuffNone | 없음 |
| 1 | BuffAttack | 공격력 배율 |
| 2 | BuffDefense | 방어력 배율 |
| 3 | BuffSpeed | 이동속도 배율 |
| 4 | BuffAttackSpeed | 공격속도 배율 |

## UI 연동 구조
- `MyPlayerController.OnStatInfoUpdate: Action<StatInfo>` — 초기 스탯 수신 시 발동
- `MyPlayerController.OnBuffApplied: Action<BuffType, float>` — 버프 수신 시 발동
- `UI_Store.OnGoldUpdate: Action<long>` — 골드 변경 시 발동
- `ObjectService.Add()` 에서 `MyPlayerController.SetPendingStatInfo()` 호출 → `_pendingStatInfo` 저장 후 Invoke (UI 생성 전 패킷 대비 버퍼)

## 알려진 수정 완료 사항
- `ObjectService.Add()`: `OnStatInfoUpdate?.Invoke()` → `SetPendingStatInfo()` 교체 (StatInfo 0 표시 버그 수정)

## 현재 수정 대상 버그
### 버그 1: UI_StatusBox 방어력(_shield)만 표시 오류
- 증상: 공격력·속도·공격속도는 정상, 방어력만 잘못된 값(0 등)
- 의심 지점: `BuffType.BuffDefense` 처리 경로, `StatInfo`에 Defense 필드 존재 여부, `UI_StatusBox._shield` 초기화 경로

### 버그 2: Teleport 스킬 미작동
- 증상: 텔레포트 카드 사용 시 정상 작동하지 않음
- 의심 지점: 텔레포트 전용 패킷 유무, `S_SKILL` 처리 분기, 클라이언트 위치 강제 이동 로직

## 진입 파일 목록 (현황 파악 시 우선 읽을 것)
### 버그 1
- `GW2_Client\Assets\Scripts\UI\SubItem\UI_StatusBox.cs`
- `GW2_Client\Assets\Scripts\Controllers\BaseController.cs`
- `GW2_Client\Assets\Scripts\Controllers\MyPlayerController.cs`
- `GW2_Client\Assets\Scripts\Packet\PacketHandler.cs`
- `GW2_Server\GW2_Server\Struct.pb.h` (StatInfo 필드 확인)

### 버그 2
- `GW2_Client\Assets\Scripts\Packet\PacketHandler.cs`
- `GW2_Client\Assets\Scripts\Controllers\MyPlayerController.cs`
- `GW2_Server\GW2_Server\Room.cpp` (텔레포트 스킬 처리)
- `GW2_Server\GW2_Server\Data\Stats.json` (텔레포트 카드 정의)
