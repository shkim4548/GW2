#pragma once
#include "pch.h"
namespace Navigation { struct LaneRoute; class WalkableGrid; }

class LaneRouteLoader
{
public:
	// path : laneRoute.json 파일의 경로
	static bool LoadLaneRoutesFromJson(const string& path, unordered_map<int32, shared_ptr<Navigation::LaneRoute>>& OUT routes);
	void BakeLaneRoutesToGrid(const unordered_map<int32, shared_ptr<Navigation::LaneRoute>>& routes, Navigation::WalkableGrid& grid);
};

