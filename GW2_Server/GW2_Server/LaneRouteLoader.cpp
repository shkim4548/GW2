#include "pch.h"
#include "LaneRouteLoader.h"
#include "NavigationSystem.h"
#include <fstream>
#include <sstream>
#include <nlohmann/json.hpp>
#include "Lobby.h"

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
    shared_ptr<Navigation::NavigationSystem> navSystem = GLobby->GetNavigationSystem().lock();

    // 0. 전체 laneId 초기화
    for (auto& cell : grid.cells)
        cell.laneId = 0;

    if (routes.empty())
    {
        GConsoleLogger->WriteStdOut(Color::YELLOW,
            L"[LaneRouteLoader::BakeLaneRoutesToGrid] routes empty. No lanes baked.\n");
        return;
    }

    GConsoleLogger->WriteStdOut(Color::YELLOW,
        L"[LaneRouteLoader::BakeLaneRoutesToGrid] Start baking %zu lanes\n",
        routes.size());

    // 1. 각 lane별로 처리
    for (const auto& kv : routes)
    {
        const int32 laneId = kv.first;
        const std::shared_ptr<Navigation::LaneRoute>& route = kv.second;

        if (route == nullptr)
        {
            GConsoleLogger->WriteStdOut(Color::RED,
                L"[LaneRouteLoader::BakeLaneRoutesToGrid] laneId=%d route is nullptr. Skip.\n",
                laneId);
            continue;
        }

        const uint8 laneIdByte = static_cast<uint8>(laneId);
        const auto& wps = route->waypoints;

        if (wps.size() < 2)
        {
            GConsoleLogger->WriteStdOut(Color::YELLOW,
                L"[LaneRouteLoader::BakeLaneRoutesToGrid] laneId=%d has %zu waypoint(s). Need >= 2. Skip.\n",
                laneId, wps.size());
            continue;
        }

        GConsoleLogger->WriteStdOut(Color::WHITE,
            L"[LaneRouteLoader::BakeLaneRoutesToGrid] laneId=%d, waypoints=%zu\n",
            laneId, wps.size());

        // 2. 연속된 웨이포인트 쌍마다 NavGrid 기준 A*를 돌려 corridor를 만든다.
        for (size_t i = 1; i < wps.size(); ++i)
        {
            const GameMath::Vector3& from = wps[i - 1];
            const GameMath::Vector3& to = wps[i];

            int32 sx = 0, sz = 0;
            int32 ex = 0, ez = 0;

            // 2-1. World -> Grid 변환 (NavSystem 기준)
            if (!navSystem->WorldToGrid(grid, from._x, from._z, sx, sz))
            {
                GConsoleLogger->WriteStdOut(Color::RED,
                    L"[LaneRouteLoader::BakeLaneRoutesToGrid] laneId=%d segment(%zu) WorldToGrid failed for start (%.2f, %.2f)\n",
                    laneId, i, from._x, from._z);
                continue;
            }

            if (!navSystem->WorldToGrid(grid, to._x, to._z, ex, ez))
            {
                GConsoleLogger->WriteStdOut(Color::RED,
                    L"[LaneRouteLoader::BakeLaneRoutesToGrid] laneId=%d segment(%zu) WorldToGrid failed for end (%.2f, %.2f)\n",
                    laneId, i, to._x, to._z);
                continue;
            }

            // 2-2. NavGrid 전체 기준으로 경로 탐색 (allowedLaneId = 0)
            std::vector<Navigation::GridCell*> segPath;
            bool ok = navSystem->FindPath(grid, sx, sz, ex, ez, segPath, /*allowedLaneId=*/0);

            if (!ok || segPath.empty())
            {
                GConsoleLogger->WriteStdOut(Color::RED,
                    L"[LaneRouteLoader::BakeLaneRoutesToGrid] laneId=%d segment(%zu) FindPath failed from (%d,%d) to (%d,%d)\n",
                    laneId, i, sx, sz, ex, ez);
                // 구조를 깨지 않기 위해 여기서 임의의 직선 corridor를 만들지는 않는다.
                // NavGrid/웨이포인트 데이터를 수정해야 할 케이스로 취급.
                continue;
            }

            // 2-3. FindPath 결과에 포함된 셀들을 laneId로 마킹
            for (Navigation::GridCell* cell : segPath)
            {
                if (cell == nullptr)
                    continue;

                // 혹시라도 다른 laneId가 이미 칠려있다면, 로그로만 남기고 덮어쓸지/유지할지 정책 결정
                if (cell->laneId != 0 && cell->laneId != laneIdByte)
                {
                    GConsoleLogger->WriteStdOut(Color::YELLOW,
                        L"[LaneRouteLoader::BakeLaneRoutesToGrid] laneId conflict: cell(x=%d,z=%d) has laneId=%d, overwrite to %d\n",
                        cell->x, cell->z, cell->laneId, laneIdByte);
                    // 정책 1) 그대로 덮어쓰기
                    // 정책 2) 첫 lane을 유지하고 Skip
                    // 여기서는 간단히 덮어쓰기 선택
                }

                cell->laneId = laneIdByte;
            }
        }
    }

    GConsoleLogger->WriteStdOut(Color::YELLOW,
        L"[LaneRouteLoader::BakeLaneRoutesToGrid] Baking lanes complete\n");
}
