# 04. 버프 시스템

## 개요

버프는 카드 사용 시 서버에서 계산·적용되며, 클라이언트는 UI 표시와 이동속도 반영만 담당합니다.  
서버가 권위를 가지며, 버프 타이머 만료도 서버에서 처리합니다.

- 서버 적용: `GW2_Server/GW2_Server/Room.cpp:233` / `Player.cpp:70`
- 서버 데이터: `Player.h:62` (버프 필드)
- 클라이언트 수신: `PacketHandler.cs:464` → `BaseController.ApplyBuff()` → `UI_StatusBox`

---

## BuffType Enum

| 값 | 이름 | Stats.json buff_value 의미 | 서버 적용 변수 |
|----|------|---------------------------|---------------|
| 0 | BuffNone | - | - |
| 1 | BuffAttack | 배율 (예: 1.5 = 150%) | `Player._attackMult` |
| 2 | BuffDefense | 감소 비율 (예: 0.3 = 30% 감소) | `Player._defenseReduct` |
| 3 | BuffSpeed | 배율 (예: 2.0 = 200%) | `Player._speedMult` |
| 4 | BuffAttackSpeed | 쿨타임 배율 (예: 0.5 = 50%) | `Player._attackSpeedMult` |

---

## 서버 버프 적용

**파일**: `Room.cpp:233-266`

```cpp
if (cardStat.buffType != 0 && cardStat.buffValue > 0.0f)
{
    switch (cardStat.buffType)
    {
    case 1: // BuffAttack
        player->_attackMult = cardStat.buffValue;
        player->_attackBuffTimer = cardStat.duration;
        break;
    case 2: // BuffDefense
        player->_defenseReduct = cardStat.buffValue;
        player->_defenseBuffTimer = cardStat.duration;
        break;
    case 3: // BuffSpeed
        player->_speedMult = cardStat.buffValue;
        player->_speedBuffTimer = cardStat.duration;
        break;
    case 4: // BuffAttackSpeed
        player->_attackSpeedMult = cardStat.buffValue;
        player->_attackSpeedBuffTimer = cardStat.duration;
        break;
    }
    // S_BUFF_APPLIED → 해당 플레이어에게만 전송
    session->Send(S_BUFF_APPLIED);
}
```

> 버프는 대상 플레이어 세션에게만 전송됩니다 (브로드캐스트 아님).

---

## 서버 버프 타이머 만료

**파일**: `Player.cpp:70-104`

매 틱(`UpdateController`) 타이머를 감소시키고, 만료 시 기본값으로 복원합니다.

```cpp
if (_attackBuffTimer > 0.0f) {
    _attackBuffTimer -= deltaTime;
    if (_attackBuffTimer <= 0.0f) { _attackMult = 1.0f; }
}
if (_defenseBuffTimer > 0.0f) {
    _defenseBuffTimer -= deltaTime;
    if (_defenseBuffTimer <= 0.0f) { _defenseReduct = 0.0f; }
}
if (_speedBuffTimer > 0.0f) {
    _speedBuffTimer -= deltaTime;
    if (_speedBuffTimer <= 0.0f) { _speedMult = 1.0f; }
}
if (_attackSpeedBuffTimer > 0.0f) {
    _attackSpeedBuffTimer -= deltaTime;
    if (_attackSpeedBuffTimer <= 0.0f) { _attackSpeedMult = 1.0f; }
}
```

| 버프 | 기본값 | 만료 시 복원값 |
|------|--------|---------------|
| `_attackMult` | 1.0f | 1.0f |
| `_defenseReduct` | 0.0f | 0.0f |
| `_speedMult` | 1.0f | 1.0f |
| `_attackSpeedMult` | 1.0f | 1.0f |

---

## 서버 버프 실제 계산

### 공격 버프 (BuffAttack)
**파일**: `Room.cpp:424` (HandleSkill 타겟 분기)

```cpp
uint64 damage = cardStat.damage + attacker->GetStatInfo().attack() * cardStat.damage_coeff;
// _attackMult는 이동 계산에서 곱해짐 (Player::UpdateController)
```

### 방어 버프 (BuffDefense)
**파일**: `Room.cpp:488`

```cpp
if (targetPlayer && targetPlayer->_defenseReduct > 0.0f)
    damage = static_cast<uint64>(damage * (1.0f - targetPlayer->_defenseReduct));
// 예: _defenseReduct=0.3 → 피해를 70%로 감소
```

### 이동속도 버프 (BuffSpeed)
**파일**: `Player.cpp:119`

```cpp
float remainMoveDist = _moveSpeed * _speedMult * deltaTime;
// _speedMult가 2.0이면 이동거리 2배
```

### 공격속도 버프 (BuffAttackSpeed)
**파일**: `Room.cpp:380`

```cpp
float interval = player->_attackInterval / player->_attackSpeedMult;
player->_attackCooldown = interval;
// _attackSpeedMult=0.5 → 쿨타임 절반 (공격 2배 빠름)
```

---

## 클라이언트 버프 수신 흐름

```
S_BUFF_APPLIED 수신
       ↓
PacketHandler.S_BUFF_APPLIEDHandler()   [PacketHandler.cs:464]
       ↓
BaseController.ApplyBuff(buffType, value, duration)
  └─ BuffSpeed: _moveSpeed *= value (이동속도 즉시 반영)
  └─ 나머지: 서버가 계산 담당, 클라이언트는 패스
       ↓
MyPlayerController.OnBuffApplied?.Invoke(buffType, value)  [내 플레이어만]
       ↓
UI_StatusBox.HandleBuffApplied()
  ├─ BuffAttack:      _attack *= value
  ├─ BuffDefense:     _shield = value * 100f   (0.30 → "30" 표시)
  ├─ BuffAttackSpeed: _attackSpeed = value
  └─ BuffSpeed:       _speed *= value
```

---

## UI_StatusBox 표시 규칙

**파일**: `GW2_Client/Assets/Scripts/UI/SubItem/UI_StatusBox.cs`

| 필드 | 초기값 | 초기화 출처 | 버프 처리 |
|------|--------|-------------|-----------|
| `_attack` | 0f | `stat.Attack` (S_ENTER_GAME) | `*= value` |
| `_shield` | 0f | 없음 (버프 시에만 표시) | `= value * 100f` |
| `_attackSpeed` | 1f | 하드코딩 | `= value` |
| `_speed` | 0f | `stat.Speed` (S_ENTER_GAME) | `*= value` |

> `_shield`는 StatInfo에 defense 필드가 없으므로 버프 미적용 상태에서는 0이 정상입니다.

---

## 버프 중복 적용 주의

현재 서버는 버프를 **덮어쓰기(overwrite)** 방식으로 처리합니다.  
같은 종류의 버프를 연속 사용하면 타이머만 갱신되고 수치는 마지막 값으로 대체됩니다.

```cpp
player->_attackMult = cardStat.buffValue;    // 이전 값 덮어씀
player->_attackBuffTimer = cardStat.duration; // 타이머 초기화
```

스택(누적) 방식이 아니므로, 같은 버프 카드를 여러 장 사용해도 효과는 1회만 적용됩니다.
