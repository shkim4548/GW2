#pragma once
struct UnitStat
{
	uint64 hp = 0;
	uint64 maxHp = 0;
	uint32 attackDamage = 0;
	float attackRange = 0.0f;
	float attackInterval = 1.0f;
	float moveSpeed = 0.0f;
	float detectionRange = 0.0f;
};

struct CardStat
{

    // === 식별 ===
    int32       id;
    string      skillId;        // "police_first", "monk_second" 등
    //SkillType   skillType;      // SKILL_ID_FIRST / SECOND

    // === 데미지 ===
    uint32      damage;         // 기본 데미지
    float       damageCoeff;    // 공격력 배율 (damage + atk * coeff)

    // === 사거리/범위 ===
    float       range;          // 최대 사거리
    float       aoeRadius;      // 범위 공격 반경 (0이면 단일 대상)

    // === 타이밍 ===
    float       cooldown;       // 쿨타임 (초)
    float       castTime;       // 시전 시간 (즉발=0)
    float       duration;       // 지속 효과 시간 (0이면 즉발 소멸)

    // === 투사체 ===
    float       projectileSpeed;  // 히트스캔=0, 투사체>0

    // === 효과 ===
    float       slowAmount;     // 이동속도 감소율 (0~1)
    float       knockbackForce; // 넉백 거리

    uint32       price;
};

class StatLoader
{
public:
	bool LoadUnitStatsFromJson(const string& path, unordered_map<string, UnitStat>& OUT outStats);
    bool LoadCardStatsFromJson(const string& path, unordered_map<int32, CardStat>& OUT outStats);
};

