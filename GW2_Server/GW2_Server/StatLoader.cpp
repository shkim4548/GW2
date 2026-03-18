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
        if (u.contains("hp"))               stat.hp = u["hp"].get<uint64_t>();
        if (u.contains("max_hp"))           stat.maxHp = u["max_hp"].get<uint64_t>();
        if (u.contains("attack_damage"))    stat.attackDamage = u["attack_damage"].get<uint32_t>();
        if (u.contains("attack_range"))     stat.attackRange = u["attack_range"].get<float>();
        if (u.contains("attack_interval"))  stat.attackInterval = u["attack_interval"].get<float>();
        if (u.contains("move_speed"))       stat.moveSpeed = u["move_speed"].get<float>();
        if (u.contains("detection_range"))  stat.detectionRange = u["detection_range"].get<float>();

        outStats[type] = stat;
        for (unordered_map<string, UnitStat>::iterator iter = outStats.begin(); iter != outStats.end(); ++iter)
        {
            cout << iter->first << ' ' << iter->second.maxHp << endl;
            cout << iter->first << ' ' << iter->second.hp << endl;
            cout << iter->first << ' ' << iter->second.moveSpeed << endl;
            cout << iter->first << ' ' << iter->second.detectionRange << endl;
        }
        GConsoleLogger->WriteStdErr(Color::GREEN, L"[StatLoader] Loaded type=%S hp=%llu atk=%u range=%.1f\n", type.c_str(), stat.hp, stat.attackDamage, stat.attackRange);
    }
    return !outStats.empty();
}

bool StatLoader::LoadCardStatsFromJson(const string& path, unordered_map<string, UnitStat>& outStats)
{
    outStats.clear();
    ifstream ifs(path);

    if (!ifs.is_open())
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[StatLoader] Failed to open%s\n", path.c_str());
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

       /* CardStat stat;
        if (u.contains("hp"))               stat.hp = u["hp"].get<uint64_t>();
        if (u.contains("max_hp"))           stat.maxHp = u["max_hp"].get<uint64_t>();
        if (u.contains("attack_damage"))    stat.attackDamage = u["attack_damage"].get<uint32_t>();
        if (u.contains("attack_range"))     stat.attackRange = u["attack_range"].get<float>();
        if (u.contains("attack_interval"))  stat.attackInterval = u["attack_interval"].get<float>();
        if (u.contains("move_speed"))       stat.moveSpeed = u["move_speed"].get<float>();
        if (u.contains("detection_range"))  stat.detectionRange = u["detection_range"].get<float>();

        outStats[type] = stat;
        GConsoleLogger->WriteStdErr(Color::GREEN, L"[StatLoader] Loaded type=%S hp=%llu atk=%u range=%.1f\n", type.c_str(), stat.hp, stat.attackDamage, stat.attackRange);*/
    }
    return !outStats.empty();
}
