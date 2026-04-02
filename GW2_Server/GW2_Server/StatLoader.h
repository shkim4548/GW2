#pragma once
struct UnitStat 
{
    uint64 hp = 0;
    uint64 maxHp = 0;
    uint32 attackDamage = 0;
    float attackRange = 0.0f;
    float attackInterval = 1.0f;
    float attackSpeed = 1.0f;    // 추가
    float moveSpeed = 0.0f;
    float detectionRange = 0.0f;
    uint32 defense = 0;          // 추가
    float healthRegen = 0.0f;    // 추가
    float shield = 0.0f;         // 추가
    float maxMana = 0.0f;        // 추가
    float manaRegen = 0.0f;      // 추가
};


struct CardStat
{

    int32       id = 0;
    string      skillId = "";

    uint32      damage = 0;
    float       damageCoeff = 0.0f;

    float       range = 0.0f;
    float       aoeRadius = 0.0f;
    int32       aoeType = 0;
    float       angle = 0.0f;

    float       cooldown = 0.0f;
    float       castTime = 0.0f;
    float       duration = 0.0f;

    float       projectileSpeed = 0.0f;
    float       slowAmount = 0.0f;
    float       knockbackForce = 0.0f;

    uint32      heal = 0;
    int32       buffType = 0;
    float       buffValue = 0.0f;

    uint32      price = 0;

};

class StatLoader
{
public:
	bool LoadUnitStatsFromJson(const string& path, unordered_map<string, UnitStat>& OUT outStats);
    bool LoadCardStatsFromJson(const string& path, unordered_map<int32, CardStat>& OUT outStats);
};

