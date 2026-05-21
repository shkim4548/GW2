# 03. 카드 시스템

## 개요

GW2의 스킬 시스템은 **카드** 기반입니다.  
플레이어는 덱에서 카드를 드로우하여 핸드에 들고, Q/W/E/R 키로 사용합니다.

- 카드 정의: `GW2_Server/Data/Stats.json` (서버 로드), `GW2_Client/Assets/Resources/Json/Stats.json` (클라이언트 동일 파일)
- 서버 처리: `GW2_Server/GW2_Server/Room.cpp` → `HandleSkill()`
- 덱/핸드 관리: `GW2_Server/GW2_Server/CardManager.cpp/h`
- 서버 스탯 파싱: `GW2_Server/GW2_Server/StatLoader.cpp/h`

---

## Stats.json 카드 스키마

```json
{
  "id": 128,
  "skill_id": "Teleporting",
  "damage": 0,
  "damage_coeff": 0.0,
  "range": 8.0,
  "cooldown": 12.0,
  "price": 150,
  "heal": 0,
  "aoe_radius": 0.0,
  "aoe_type": 0,
  "angle": 0.0,
  "buff_type": 0,
  "buff_value": 0.0,
  "duration": 0.0,
  "target_type": 1,
  "spawn_type": "ClickPoint"
}
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | int | 카드 고유 ID (101~) |
| `skill_id` | string | 이펙트 식별자 (클라이언트 이펙트 매핑용) |
| `damage` | int | 기본 피해량 |
| `damage_coeff` | float | 공격력 계수 (실제 피해 = damage + attack × coeff) |
| `range` | float | 사거리 |
| `cooldown` | float | 재사용 대기 시간 (초) |
| `price` | int | 구매 골드 |
| `heal` | int | 회복량 (0이면 생략 가능) |
| `aoe_radius` | float | AOE 반경 (0이면 단일 대상) |
| `aoe_type` | int | 0=없음, 1=원형, 2=원뿔형 |
| `angle` | float | 원뿔형 범위 각도 (도) |
| `buff_type` | int | BuffType enum 값 (0이면 버프 없음) |
| `buff_value` | float | 버프 수치 |
| `duration` | float | 버프/효과 지속 시간 (초) |
| `target_type` | int | 0=자신, 1=클릭 지점, 2=클릭 대상 |
| `spawn_type` | string | 이펙트 스폰 방식 (클라이언트 전용) |

---

## target_type 분류

| 값 | 의미 | 클라이언트 동작 | C_SKILL target_id |
|----|------|-----------------|-------------------|
| 0 | Self | 즉시 발동 | 0 |
| 1 | ClickPoint | 좌클릭 지점 선택 대기 | 0 (지점만 pos_x, pos_z로 전달) |
| 2 | ClickTarget | 좌클릭 적 대상 선택 대기 | 대상 object_id |

---

## 서버 HandleSkill 흐름

**파일**: `GW2_Server/GW2_Server/Room.cpp:189`

```
C_SKILL 수신
    │
    ├─ target_id == 0 (Self / ClickPoint)
    │       │
    │       ├─ skillId == 1 → return false (기본공격은 타겟 필수)
    │       ├─ 카드 소유 확인 (CardManager::HasCard)
    │       ├─ 카드 사용 처리 (CardManager::UseCard)
    │       ├─ 카드 드로우 → S_DRAW_CARD 전송
    │       ├─ [Heal > 0] → Heal() → S_HP_CHANGE 브로드캐스트
    │       ├─ [BuffType != 0] → Player 버프 적용 → S_BUFF_APPLIED 전송
    │       ├─ S_SKILL 브로드캐스트 (이펙트용)
    │       └─ [buffType==0 && damage==0 && heal==0] → Mobility(텔레포트)
    │               └─ 목적지 즉시 이동 → S_MOVE_END 브로드캐스트
    │
    └─ target_id != 0 (ClickTarget)
            ├─ skillId == 1 → 공격 쿨다운 검증
            ├─ 대상 존재 확인
            ├─ 팀 체크 (아군 공격 불가)
            ├─ 사망 확인
            ├─ 거리 체크 (사거리 초과 시 return false)
            ├─ 데미지 계산
            │   └─ damage = cardStat.damage + attack × coeff
            │   └─ 방어력 감소 적용: damage × (1 - _defenseReduct)
            ├─ S_HP_CHANGE 브로드캐스트
            └─ 사망 처리 → HandleRemoveObject()
```

---

## Mobility 카드 조건

**파일**: `Room.cpp:347`

```cpp
if (cardStat.buffType == 0 && cardStat.damage == 0 && cardStat.heal == 0)
{
    // 목적지로 즉시 이동
    player->SetPosVector(dest);
    player->SetMoveState(MOVE_STATE_IDLE);
    Broadcast(S_MOVE_END);
}
```

> **주의**: `buff_type` 필드가 0이 아니면 Mobility 로직이 실행되지 않습니다.  
> 텔레포트 카드(id 128)의 `buff_type`은 반드시 0이어야 합니다.

---

## AOE 처리

**파일**: `Room.cpp:276`

### aoe_type 1 — 원형 AOE
```cpp
GameMath::Vector3 center(skillPkt.pos_x(), 0.f, skillPkt.pos_z());
// center 기준 aoeRadius 이내의 적팀 오브젝트 전부 피해
```

### aoe_type 2 — 원뿔형 AOE
```cpp
GameMath::Vector3 dir(skillPkt.dir_x(), 0.f, skillPkt.dir_z());
float halfAngleRad = (cardStat.angle * 0.5f) * (PI / 180.f);
// 시전자 기준 dir 방향, angle 범위 내, aoeRadius 이내 적팀 오브젝트 피해
```

---

## 카드 목록 (Stats.json 기준)

| ID | 이름 | target | 효과 |
|----|------|--------|------|
| 101 | AmuletOfSteel | Self | 방어 30% / 8초 |
| 102 | Armor | Self | 방어 35% / 10초 |
| 103 | BloodCoin | Self | (특수 효과) |
| 104 | BloodTransfusion | Self | 회복 150 |
| 105 | Cannon | ClickPoint | AOE 피해 120 (원형 r=2.5) |
| 106 | Charge | ClickTarget | 돌진 |
| 107 | CrisisAversion | Self | 방어 50% / 15초 |
| 108 | DeadlySpeed | Self | 이속 2배 / 8초 |
| 109 | EnergyAmplification | Self | 공격 1.5배 / 12초 |
| 110 | FatalAttack | ClickTarget | 단일 피해 100 |
| 111 | Grenade | ClickPoint | AOE 피해 80 (원형 r=5) |
| 112 | HackingGrenade | ClickTarget | 피해 + 스턴 2초 |
| 113 | HealthKit | Self | 회복 100 |
| 114 | IceJail | ClickTarget | 스턴 3초 |
| 115 | Infection | ClickTarget | 단일 피해 30 |
| 116 | InvincibleShield | Self | 방어 70% / 20초 |
| 117 | InvincibleWeapon | Self | 공격 1.8배 / 15초 |
| 118 | Lava | ClickPoint | AOE 피해 90 (원형 r=3.5) |
| 119 | MissileBomb | ClickPoint | AOE 피해 130 (원형 r=5) |
| 120 | Potion | Self | 회복 80 |
| 121 | Purify | Self | (특수 효과) |
| 122 | Resurrection | Self | 회복 300 |
| 123 | Shield | Self | 방어 30% / 10초 |
| 124 | ShiningCrystal | Self | 공격 1.3배 / 8초 |
| 125 | Spear | ClickTarget | 단일 피해 60 |
| 126 | Speed | Self | 이속 1.5배 / 10초 |
| 127 | Strengthen | Self | 공격 1.4배 / 10초 |
| 128 | Teleporting | ClickPoint | 텔레포트 |
| 129 | WindBlade | ClickPoint | 원뿔 AOE 피해 70 (각도 60°) + 이속 2배 |
| 130 | WingsOfTheBattlefield | Self | 공격속도 0.5배 / 8초 |

---

## 덱 / 핸드 관리

**파일**: `GW2_Server/GW2_Server/CardManager.cpp/h`

- `Player._deck`: 보유 카드 ID 목록
- `Player._hand`: 현재 핸드 (최대 4장)
- 카드 사용 시: `CardManager::UseCard()` → 핸드에서 제거 → 덱에서 드로우 → `S_DRAW_CARD` 전송
- 핸드 전체 동기화: `S_HAND_SYNC`
