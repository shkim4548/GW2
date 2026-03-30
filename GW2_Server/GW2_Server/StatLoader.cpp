#include "pch.h"
#include <fstream>
#include <nlohmann/json.hpp>
#include "StatLoader.h"

bool StatLoader::LoadUnitStatsFromJson(const string& path, unordered_map<string, UnitStat>& OUT outStats)
{
    outStats.clear();
    ifstream ifs(path);
    
    if (!ifs.is_open())
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[StatLoader] Failed to open%s \n", path.c_str());
        return false;
    }

    nlohmann::json root;
    try{ ifs >> root; }
    catch (const exception& e)
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[StatLoader] JSON parse Error\n");
        return false;
    }

    if (!root.contains("units") || !root["units"].is_array())
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[StatLoader] 'units' array not found\n");
        return false;
    }

    for (const auto& u : root["units"])
    {
        if (!u.contains("type") || !u["type"].is_string())
            continue;

        string type = u["type"].get<string>();

        UnitStat stat;
        // ── 기존 필드 ──────────────────────────────
        if (u.contains("hp"))               stat.hp = u["hp"].get<uint64_t>();
        if (u.contains("max_hp"))           stat.maxHp = u["max_hp"].get<uint64_t>();
        if (u.contains("attack_damage"))    stat.attackDamage = u["attack_damage"].get<uint32_t>();
        if (u.contains("attack_range"))     stat.attackRange = u["attack_range"].get<float>();
        if (u.contains("attack_interval"))  stat.attackInterval = u["attack_interval"].get<float>();
        if (u.contains("move_speed"))       stat.moveSpeed = u["move_speed"].get<float>();
        if (u.contains("detection_range"))  stat.detectionRange = u["detection_range"].get<float>();

        // ── 추가된 필드 ────────────────────────────
        if (u.contains("attack_speed"))     stat.attackSpeed = u["attack_speed"].get<float>();
        if (u.contains("defense"))          stat.defense = u["defense"].get<uint32_t>();
        if (u.contains("health_regen"))     stat.healthRegen = u["health_regen"].get<float>();
        if (u.contains("shield"))           stat.shield = u["shield"].get<float>();
        if (u.contains("max_mana"))         stat.maxMana = u["max_mana"].get<float>();
        if (u.contains("mana_regen"))       stat.manaRegen = u["mana_regen"].get<float>();

        outStats[type] = stat;
        for (unordered_map<string, UnitStat>::iterator iter = outStats.begin(); iter != outStats.end(); ++iter)
        {
            cout << iter->first << ' ' << iter->second.hp << endl;
            cout << iter->first << ' ' << iter->second.moveSpeed << endl;
            cout << iter->first << ' ' << iter->second.detectionRange << endl;
        }
        GConsoleLogger->WriteStdErr(Color::GREEN, L"[StatLoader] Loaded type=%S hp=%llu atk=%u range=%.1f\n", type.c_str(), stat.hp, stat.attackDamage, stat.attackRange);
    }
    return !outStats.empty();
}

bool StatLoader::LoadCardStatsFromJson(const string& path, unordered_map<int32, CardStat>& OUT outStats)
{
    outStats.clear();
    ifstream ifs(path);
    if (!ifs.is_open())
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[StatLoader] Failed to open %s\n", path.c_str());
        return false;
    }

    nlohmann::json root;
    try {
        ifs >> root;
    }
    catch (const exception& e)
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[StatLoader] JSON parse Error\n");
        return false;
    }

    if (!root.contains("cards") || !root["cards"].is_array())
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[StatLoader] 'cards' array not found\n");
        return false;
    }

    for (const auto& c : root["cards"])
    {
        if (!c.contains("id")) continue;

        CardStat stat;
        stat.id = c["id"].get<int32_t>();
        if (c.contains("damage_coeff"))      stat.damageCoeff = c["damage_coeff"].get<float>();
        if (c.contains("cast_time"))         stat.castTime = c["cast_time"].get<float>();
        if (c.contains("duration"))          stat.duration = c["duration"].get<float>();
        if (c.contains("projectile_speed"))  stat.projectileSpeed = c["projectile_speed"].get<float>();
        if (c.contains("price"))             stat.price = c["price"].get<uint32_t>();
        if (c.contains("skill_id"))          stat.skillId = c["skill_id"].get<string>();
        if (c.contains("damage"))           stat.damage = c["damage"].get<uint32_t>();
        if (c.contains("range"))            stat.range = c["range"].get<float>();
        if (c.contains("cooldown"))         stat.cooldown = c["cooldown"].get<float>();
        if (c.contains("heal"))             stat.heal = c["heal"].get<uint32_t>();
        if (c.contains("buff_type"))  stat.buffType = c["buff_type"].get<int32_t>();
        if (c.contains("buff_value")) stat.buffValue = c["buff_value"].get<float>();
        if (c.contains("aoe_radius")) stat.aoeRadius = c["aoe_radius"].get<float>();
        if (c.contains("aoe_type"))   stat.aoeType = c["aoe_type"].get<int32_t>();
        if (c.contains("angle"))      stat.angle = c["angle"].get<float>();


        outStats[stat.id] = stat;
        GConsoleLogger->WriteStdOut(Color::GREEN,
            L"[StatLoader] Card id=%d dmg=%u range=%.1f cd=%.1f\n",
            stat.id, stat.damage, stat.range, stat.cooldown);
    }
    return !outStats.empty();
}
