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

class StatLoader
{
public:
	bool LoadUnitStatsFromJson(const string& path, unordered_map<string, UnitStat>& OUT outStats);
};

