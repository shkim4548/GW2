#include "pch.h"
#include "LaneRouteLoader.h"
#include "NavigationSystem.h"
#include <fstream>
#include <sstream>
#include <nlohmann/json.hpp>

bool LaneRouteLoader::LoadLaneRoutesFromJson(const string& path, unordered_map<int32, shared_ptr<Navigation::LaneRoute>>& routes)
{
	routes.clear();
    ifstream ifs(path);

    if (!ifs.is_open())
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneRouteLoader] Failed to open file: %S\n", path.c_str());
        return false;
    }

    nlohmann::json root;
    try
    {
        ifs >> root;
    }
    catch (const std::exception& e)
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneRouteLoader] JSON parse error: %S\n", e.what());
        return false;
    }

    // 최상위에 "lanes" 배열 필수
    if (!root.contains("lanes") || !root["lanes"].is_array())
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneRouteLoader] Invalid format: 'lanes' array not found.\n");
        return false;
    }

    const nlohmann::json& lanesJson = root["lanes"];
    int32 loadedCount = 0;

    for (const auto& laneJson : lanesJson)
    {
        if (!laneJson.contains("laneId") || !laneJson["laneId"].is_number_integer())
        {
            GConsoleLogger->WriteStdErr(Color::YELLOW,
                L"[LaneRouteLoader] Skip lane: 'laneId' missing or not integer.\n");
            continue;
        }

        int32 laneId = laneJson["laneId"].get<int32>();
        if (laneId <= 0)
        {
            // 정책: 0 이하는 INVALID
            GConsoleLogger->WriteStdErr(Color::YELLOW,
                L"[LaneRouteLoader] Skip lane: invalid laneId=%d\n", laneId);
            continue;
        }

        if (!laneJson.contains("waypoints") || !laneJson["waypoints"].is_array())
        {
            GConsoleLogger->WriteStdErr(Color::YELLOW,
                L"[LaneRouteLoader] Skip laneId=%d: 'waypoints' array not found.\n", laneId);
            continue;
        }

        const nlohmann::json& wpsJson = laneJson["waypoints"];
        if (wpsJson.empty())
        {
            GConsoleLogger->WriteStdErr(Color::YELLOW,
                L"[LaneRouteLoader] Skip laneId=%d: 'waypoints' is empty.\n", laneId);
            continue;
        }

        std::shared_ptr<Navigation::LaneRoute> route = std::make_shared<Navigation::LaneRoute>();
        route->laneId = static_cast<uint8>(laneId);

        for (const auto& wpJson : wpsJson)
        {
            if (!wpJson.is_object())
                continue;

            float x = 0.f, y = 0.f, z = 0.f;

            if (wpJson.contains("x") && wpJson["x"].is_number())
                x = wpJson["x"].get<float>();
            if (wpJson.contains("y") && wpJson["y"].is_number())
                y = wpJson["y"].get<float>();
            if (wpJson.contains("z") && wpJson["z"].is_number())
                z = wpJson["z"].get<float>();

            GameMath::Vector3 pos;
            pos._x = x;
            pos._y = y;
            pos._z = z;

            route->waypoints.push_back(pos);
        }

        if (route->waypoints.empty())
        {
            GConsoleLogger->WriteStdErr(Color::YELLOW, L"[LaneRouteLoader] Skip laneId=%d: no valid waypoints.\n", laneId);
            continue;
        }

        auto it = routes.find(laneId);
        if (it != routes.end())
        {
            GConsoleLogger->WriteStdErr(Color::YELLOW,
                L"[LaneRouteLoader] Duplicate laneId=%d. Overwrite previous route.\n", laneId);
        }

        routes[laneId] = route;
        ++loadedCount;
    }

    if (loadedCount == 0)
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneRouteLoader] No lane routes loaded from file: %S\n", path.c_str());
        return false;
    }

    GConsoleLogger->WriteStdOut(Color::GREEN,
        L"[LaneRouteLoader] Loaded %d lane routes from %S\n", loadedCount, path.c_str());

    return true;
}
