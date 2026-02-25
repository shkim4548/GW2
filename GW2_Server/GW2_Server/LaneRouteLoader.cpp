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

void LaneRouteLoader::BakeLaneRoutesToGrid(const unordered_map<int32, shared_ptr<Navigation::LaneRoute>>& routes, Navigation::WalkableGrid& grid)
{
    shared_ptr<Navigation::NavigationSystem> navSystem;

    // 1) 초기화: 필요하면 기존 laneId를 0으로 리셋
    for (auto& cell : grid.cells)
        cell.laneId = 0;

    // 2) 각 laneRoute 순회
    for (const auto& kv : routes)
    {
        int32 laneId = kv.first;
        const shared_ptr<Navigation::LaneRoute>& route = kv.second;
        if (!route) continue;

        uint8 laneIdByte = static_cast<uint8>(laneId);

        const auto& wps = route->waypoints;
        if (wps.size() == 0)
            continue;

        // 2-1) 각 웨이포인트를 grid에 찍기
        for (size_t i = 0; i < wps.size(); ++i)
        {
            const GameMath::Vector3& wp = wps[i];

            int32 gx = 0, gz = 0;
            if (!navSystem->WorldToGrid(grid, wp._x, wp._z, gx, gz))
                continue;

            if (gx < 0 || gz < 0 || gx >= grid.width || gz >= grid.height)
                continue;

            Navigation::GridCell& cell = grid.At(gx, gz);
            cell.laneId = laneIdByte;
        }

        // 2-2) 웨이포인트 사이의 segment도 메우고 싶다면 (선 따라 찍기)
        for (size_t i = 1; i < wps.size(); ++i)
        {
            const GameMath::Vector3& prev = wps[i - 1];
            const GameMath::Vector3& cur = wps[i];

            int32 sx, sz, ex, ez;
            if (!navSystem->WorldToGrid(grid, prev._x, prev._z, sx, sz)) continue;
            if (!navSystem->WorldToGrid(grid, cur._x, cur._z, ex, ez))   continue;

            // Bresenham 혹은 단순 보간
            int dx = ex - sx;
            int dz = ez - sz;
            int steps = std::max(std::abs(dx), std::abs(dz));
            if (steps == 0) continue;

            for (int step = 0; step <= steps; ++step)
            {
                float t = (steps == 0) ? 0.0f : (float)step / (float)steps;
                int gx = sx + static_cast<int>(std::round(dx * t));
                int gz = sz + static_cast<int>(std::round(dz * t));

                if (gx < 0 || gz < 0 || gx >= grid.width || gz >= grid.height)
                    continue;

                Navigation::GridCell& c = grid.At(gx, gz);
                c.laneId = laneIdByte;
            }
        }
    }
}
