#include "pch.h"
#include "GameLogic.h"
#include "Object.h"
#include "NavigationSystem.h"

#undef min
#undef max

Navigation::NavigationSystem::NavigationSystem()
{
	_grids = WalkableGrid();
}

Navigation::NavigationSystem::~NavigationSystem()
{
	cout << "NavigationSystem Destroy" << endl;
}

// indices는 0-2-1 기준의 삼각 인덱스 배열 형태로 들어온다.
void Navigation::NavigationSystem::Build(const vector<GameMath::Vector3> vertices, const vector<int32>& indices)
{
	_allTriangles.clear();
	_allTriangles.reserve(indices.size() / 3);

	for (size_t i = 0; i < indices.size(); i += 3)
	{
		Triangle tri;
		tri.v1 = vertices[indices[i]];
		tri.v2 = vertices[indices[i + 1]];
		tri.v3 = vertices[indices[i + 2]];

		// 법선 계산
		GameMath::Vector3 e1 = tri.v2 - tri.v1;
		GameMath::Vector3 e2 = tri.v3 - tri.v1;
		GameMath::Vector3 t = e1.GameMath::Vector3::Cross(e2);
		tri.normal = GameMath::Vector3::GetNormalVector(t);

		_allTriangles.push_back(tri);
	}
}

bool Navigation::NavigationSystem::GetGroundHeight(float x, float z, float& OUT outY)
{
	Ray ray;
	ray.origin = GameMath::Vector3(x, 10000.0f, z);
	ray.dir = GameMath::Vector3(0, -1, 0);

	RaycastHit hit;
	if (!RaycastWorld(ray,	FLT_MAX, /*cullBackFace=*/false, hit))
	{
		return false;
	}

	outY = hit.position._y;
	return true;
}


bool Navigation::NavigationSystem::CanMoveStraight(GameMath::Vector3 start, GameMath::Vector3 end)
{
	GameMath::Vector3 dir = end - start;
	float length = dir.Length();

	if (length <= 0.0001f)
		return true;

	dir = GameMath::Vector3::GetNormalVector(dir);

	Ray ray;
	ray.origin = start;
	ray.dir = dir;

	RaycastHit hit;
	if (!RaycastWorld(ray,	length,	/*cullBackFace=*/true, hit))
	{
		return true;
	}

	// 바닥 판정: 법선이 위를 향하면 통과
	if (hit.normal._y > 0.7f)
		return true;

	return false;
}

bool Navigation::NavigationSystem::CanMoveStraightXZ(const GameMath::Vector3& start, const GameMath::Vector3& end)
{

	return false;
}

/*-----------------
	Raycasting
-------------------*/

optional<HitResult> Navigation::NavigationSystem::RayIntersects(const Ray& ray, const GameMath::Vector3& v1, const GameMath::Vector3& v2, const GameMath::Vector3& v3, bool cullBackFace)
{
	// 삼각형의 모서리 벡터 계산
	GameMath::Vector3 edge1 = v2 - v1;
	GameMath::Vector3 edge2 = v3 - v1;

	// ray.dir & edge2의 외적을 계산한다
	GameMath::Vector3 h = ray.dir.Cross(edge2);

	// a = e1 * h
	float a = edge1.Dot(h);

	// EPS 0에 근접한 수 판별용, 너무 작으면 거의 평행하다
	const float EPSILON = 1e-6f;

	// 레이가 뒷면을 관통할 경우, 무시
	if (cullBackFace)
	{
		// 뒷면 컬링: a가 EPS 이하이면(양수 측면만 허용) 교차 없음.
		if (a < EPSILON)
			return nullopt;
	}
	else
	{
		// 컬링을 하지 않을 경우, a가 거의 0아면 레이가 삼각형과 거의 평행
		if (fabs(a) < EPSILON)
			return nullopt;
	}

	// f = 1 / a
	float f = 1.0f / a;

	// s = O -v1
	GameMath::Vector3 s = ray.origin - v1;

	// 6) u = f * (s · h)
	float u = f * s.Dot(h);
	// 삼각형 내부에 없으면 교차가 아니다
	if (u < 0.0f || u > 1.0f)
		return nullopt;

	// q = s x e1
	GameMath::Vector3 q = s.Cross(edge1);

	// 8) v = f * (D · q)
	float v = f * ray.dir.Dot(q);
	// 삼각형 내부 또는 경계
	if (v < 0.0f || u + v > 1.0f)
		return nullopt;

	// 9) t = f * (e2 · q)
	float t = f * edge2.Dot(q);

	// t가 EPS보다 작으면, 뒤에 있거나 아주 작은 값으면 무시한다
	if (t <= EPSILON)
		return nullopt;

	// 성공 : 교차지점 계산
	HitResult hit;
	hit.t = t;
	hit.u = u;
	hit.v = v;
	hit.position = ray.origin + ray.dir * t;

	// 삼각형의 법선, 반환시 정규화 하는 것이 편리하다.
	hit.normal = GameMath::Vector3::GetNormalVector(edge1.Cross(edge2));
	return hit;
}

bool Navigation::NavigationSystem::RaycastWorld(const Ray& ray, float maxDistance, bool cullBackFace,	RaycastHit& outHit)
{
	bool hasHit = false;
	float closestT = maxDistance;

	for (const Triangle& tri : _allTriangles)
	{
		auto hit = RayIntersects(
			ray,
			tri.v1,
			tri.v2,
			tri.v3,
			cullBackFace);

		if (!hit.has_value())
			continue;

		if (hit->t < closestT)
		{
			closestT = hit->t;
			outHit.t = hit->t;
			outHit.position = hit->position;
			outHit.normal = hit->normal;
			outHit.triangle = &tri;
			hasHit = true;
		}
	}

	return hasHit;
}

bool Navigation::NavigationSystem::RaycastWorld2D(const Ray2D& ray, float maxDistance, RaycastHit2D& outHit)
{
	const float STEP = _grids.cellSize * 0.25f; // 충분히 작은 값
	float traveled = 0.0f;

	GameMath::Vector2 pos = ray.origin;

	while (traveled <= maxDistance)
	{
		// World → Grid
		int gx, gz;
		if (!WorldToGridXZ(pos._x, pos._z, gx, gz))
		{
			// Grid 밖 = 벽
			outHit.point = pos;
			outHit.distance = traveled;
			outHit.normal = GameMath::Vector2(0, 0);
			return true;
		}

		if (!_grids.At(gx, gz).walkable)
		{
			outHit.point = pos;
			outHit.distance = traveled;
			outHit.normal = GameMath::Vector2(0, 0);
			return true;
		}

		pos = pos + (ray.dir * STEP);
		traveled += STEP;
	}

	return false; // 충돌 없음
}

void Navigation::NavigationSystem::BuildWalkableGrid(WalkableGrid& grid, int32 width, int32 height, float cellSize, GameMath::Vector3 origin)
{
	// grid cell 초기화
	grid.width = width;
	grid.height = height;
	grid.cellSize = cellSize;
	grid.origin = origin;
	grid.cells.resize(width * height);

	BuildCells(grid);
	BuildConnections(grid);
}

void Navigation::NavigationSystem::BuildCells(WalkableGrid& grid)
{
	// cout << "BuildCells Start" << endl;
	// 초기화된 데이터로 실제로 맵을 만든다.
	// 벽위, 공중, 낭떠러지를 구분하고, 갈 수 있는 공간을 구분한다.
	const float MAX_SLOPE_HEIGHT = 1.0f;
	for (int32 z = 0; z < grid.height; ++z)
	{
		for (int32 x = 0; x < grid.width; ++x)
		{
			// grid cell의 중심 좌표를 world 좌표로 변환한다.
			float worldX = grid.origin._x + (x + 0.5f) * grid.cellSize;
			float worldZ = grid.origin._z + (z + 0.5f) * grid.cellSize;

			// 해당 위치의 실제 지면 높이를 샘플링한다
			float y;
			if (GetGroundHeight(worldX, worldZ, y) == false)
			{
				// 지면이 없으면 이동 불가하다
				grid.At(x, z).walkable = false;
				continue;
			}
			// 지면이 있으면 이동 가능하다.
			grid.At(x, z).walkable = true;
			grid.At(x, z).height = y;
			grid.At(x, z).x = x;
			grid.At(x, z).z = z;
		}
	}

	// 경사도 검사, 이웃과의 높이 차이 이용
	for (int32 z = 0; z < grid.height; ++z)
	{
		for (int32 x = 0; x < grid.width; ++x)
		{
			// 진입할 수 없는 지점이면 애초에 검사할 필요가 없다.
			if (grid.At(x, z).walkable == false)
			{
				continue;
			}

			if (x > 0)
			{
				float dh = fabs(grid.At(x, z).height - grid.At(x - 1, z).height);
				if (dh > MAX_SLOPE_HEIGHT)
				{
					grid.At(x, z).walkable = false;
				}
			}
		}
	}
}

void Navigation::NavigationSystem::BuildConnections(WalkableGrid& grid)
{
	for (int32 z = 0; z < grid.height; ++z)
	{
		for (int32 x = 0; x < grid.width; ++x)
		{
			GridCell& cell = grid.At(x, z);

			if (!cell.walkable)
				continue;

			// North
			if (z + 1 < grid.height && grid.At(x, z + 1).walkable)
				cell.neighbors[DIR_NORTH] = true;

			// East
			if (x + 1 < grid.width && grid.At(x + 1, z).walkable)
				cell.neighbors[DIR_EAST] = true;

			// South
			if (z > 0 && grid.At(x, z - 1).walkable)
				cell.neighbors[DIR_SOUTH] = true;

			// West
			if (x > 0 && grid.At(x - 1, z).walkable)
				cell.neighbors[DIR_WEST] = true;
		}
	}
}

bool Navigation::NavigationSystem::GridToWorld(WalkableGrid& grid, int32 x, int32 z, GameMath::Vector3& OUT worldPos)
{
	if (x < 0 || z < 0 || x >= grid.width || z >= grid.height)
		return false;

	const GridCell& cell = grid.At(x, z);
	if (!cell.walkable)
		return false;

	worldPos._x = grid.origin._x + (x + 0.5f) * grid.cellSize;
	worldPos._z = grid.origin._z + (z + 0.5f) * grid.cellSize;

	worldPos._y = 0.0f;   // 예: 0.0f 또는 Nav 기준 Y

	return true;
}

bool Navigation::NavigationSystem::WorldToGrid(const WalkableGrid& grid, GameMath::Vector3& worldPos, int32& OUT x, int32& OUT z)
{
	if (!WorldToGridImpl(grid, worldPos._x, worldPos._z, x, z))
		return false;

	if (x < 0 || z < 0 || x >= grid.width || z >= grid.height)
		return false;

	if (!grid.At(x, z).walkable)
		return false;

	return true;
}

bool Navigation::NavigationSystem::WorldToGrid(const WalkableGrid& grid, float worldX, float worldZ, int32& OUT x, int32& OUT z)
{
	if (!WorldToGridImpl(grid, worldX, worldZ, x, z))
		return false;

	if (x < 0 || z < 0 || x >= grid.width || z >= grid.height)
		return false;

	if (!grid.At(x, z).walkable)
		return false;

	return true;
}

void Navigation::NavigationSystem::BuildGrid(float cellSize)
{
	_cellSize = cellSize;

	// obj 기반 월드 범위 계산
	_mapMinX = FLT_MAX;
	_mapMaxX = -FLT_MAX;
	_mapMinZ = FLT_MAX;
	_mapMaxZ = -FLT_MAX;

	for (const Triangle& tri : _allTriangles)
	{
		for (const GameMath::Vector3& v : { tri.v1, tri.v2, tri.v3 })
		{
			_mapMinX = std::min(_mapMinX, v._x);
			_mapMaxX = std::max(_mapMaxX, v._x);
			_mapMinZ = std::min(_mapMinZ, v._z);
			_mapMaxZ = std::max(_mapMaxZ, v._z);
		}
	}

	// Grid 크기 계산
	_gridRows = static_cast<int>((_mapMaxX - _mapMinX) / _cellSize) + 1;
	_gridCols = static_cast<int>((_mapMaxZ - _mapMinZ) / _cellSize) + 1;

	_cells.clear();
	_cells.reserve(_gridRows * _gridCols);

	// 각 셀을 판정한다.
	for (int32 z = 0; z < _gridCols; ++z)
	{
		for (int32 x = 0; x < _gridRows; ++x)
		{
			float worldX = _mapMinX + x * _cellSize + _cellSize * 0.5f;
			float worldZ = _mapMinZ + z * _cellSize + _cellSize * 0.5f;

			GridCell cell;
			cell.x = x;
			cell.z = z;

			float y;
			if (GetGroundHeight(worldX, worldZ, y))
			{
				cell.walkable = true;
				cell.height = y;
			}
			else
			{
				cell.walkable = false;
				cell.height = 0.0f;
			}
			_cells.push_back(cell);
		}
	}
}

bool Navigation::NavigationSystem::WorldToGridXZ(float worldX, float worldZ, int& outX, int& outZ) const
{
	float localX = (worldX - _grids.origin._x) / _grids.cellSize;
	float localZ = (worldZ - _grids.origin._z) / _grids.cellSize;

	outX = static_cast<int>(floor(localX));
	outZ = static_cast<int>(floor(localZ));

	if (outX < 0 || outX >= _grids.width ||
		outZ < 0 || outZ >= _grids.height)
		return false;

	return true;
}

void Navigation::NavigationSystem::InitNavmesh(vector<Triangle>&& triangles, WalkableGrid&& grid)
{
	// 기존 데이터 제거
	_allTriangles.clear();
	_grids.cells.clear();

	// 소유권 이전 (copy 없음)
	_allTriangles = std::move(triangles);
	_grids = std::move(grid);

	// 방어적 검증 (디버그용)
	assert(!_allTriangles.empty());
	assert(_grids.width > 0);
	assert(_grids.height > 0);
	assert(_grids.cells.size() ==
		static_cast<size_t>(_grids.width * _grids.height));
}

void Navigation::NavigationSystem::Init(WalkableGrid& grid)
{
	_grids = move(grid);
}

bool Navigation::NavigationSystem::FindPath(const WalkableGrid& grid, int32 startX, int32 startZ, int32 endX, int32 endZ, vector<GridCell*>& outPath, uint8 allowedLaneId)
{
	outPath.clear();

	const int32 W = grid.width;
	const int32 H = grid.height;

	// (선택) start / end lane 체크
	if (allowedLaneId != 0)
	{
		const GridCell& startCell = grid.At(startX, startZ);
		const GridCell& endCell = grid.At(endX, endZ);
		//GConsoleLogger->WriteStdOut(Color::WHITE, L"[Navigation::FindPath] allowedLaneId=%d, start(%d, %d) startLaneId = %d, end(%d, %d), endLaneId = %d\n", allowedLaneId, startX, startZ, startCell.laneId, endX, endZ, endCell.laneId);
		if (startCell.laneId != allowedLaneId)
			return false;

		if (endCell.laneId != allowedLaneId)
		{
			GConsoleLogger->WriteStdErr(Color::RED, L"[FindPath] end cell lane mismatch: endLane=%d allowed=%d\n", endCell.laneId, allowedLaneId);
			return false;
		}
	}

	vector<NodeRecord> records(W * H);

	for (auto& r : records)
	{
		r.g = INT32_MAX;
		r.f = INT32_MAX;
		r.parentX = -1;
		r.parentZ = -1;
		r.opened = false;
		r.closed = false;
	}

	auto startIdx = Index(grid, startX, startZ);
	auto& startRec = records[startIdx];

	startRec.g = 0;
	startRec.f = Heuristic(startX, startZ, endX, endZ);
	startRec.opened = true;

	priority_queue<OpenNode> open;
	open.push({ startX, startZ, startRec.f });

	while (!open.empty())
	{
		OpenNode cur = open.top();
		open.pop();

		int32 cx = cur.x;
		int32 cz = cur.z;
		int32 cidx = Index(grid, cx, cz);
		NodeRecord& current = records[cidx];

		if (current.closed)
			continue;

		current.closed = true;

		// 도착
		if (cx == endX && cz == endZ)
		{
			int32 x = cx;
			int32 z = cz;

			while (!(x == startX && z == startZ))
			{
				outPath.push_back(const_cast<GridCell*>(&grid.At(x, z)));
				NodeRecord& r = records[Index(grid, x, z)];
				int32 px = r.parentX;
				int32 pz = r.parentZ;
				x = px;
				z = pz;
			}

			outPath.push_back(const_cast<GridCell*>(&grid.At(startX, startZ)));
			std::reverse(outPath.begin(), outPath.end());
			return true;
		}

		const GridCell& cell = grid.At(cx, cz);

		static const int dx[4] = { 0, 1, 0, -1 };
		static const int dz[4] = { 1, 0, -1, 0 };

		for (int dir = 0; dir < 4; ++dir)
		{
			if (!cell.neighbors[dir])
				continue;

			int32 nx = cx + dx[dir];
			int32 nz = cz + dz[dir];

			if (nx < 0 || nz < 0 || nx >= W || nz >= H)
				continue;

			int32 nidx = Index(grid, nx, nz);
			NodeRecord& nr = records[nidx];

			if (nr.closed)
				continue;

			// ★ neighbor 셀 정보 가져오기
			const GridCell& neighborCell = grid.At(nx, nz);

			// ★ walkable 체크
			if (!neighborCell.walkable)
				continue;

			// ★ lane 제한 체크
			if (allowedLaneId != 0 && neighborCell.laneId != allowedLaneId)
				continue;

			int32 newG = current.g + 1;

			if (!nr.opened || newG < nr.g)
			{
				nr.g = newG;
				nr.f = newG + Heuristic(nx, nz, endX, endZ);
				nr.parentX = cx;
				nr.parentZ = cz;
				nr.opened = true;

				open.push({ nx, nz, nr.f });
			}
		}
	}

	return false;
}

Navigation::MoveValidationResult Navigation::NavigationSystem::ValidateMove(const WalkableGrid& grid, const Object& unit, GameMath::Vector3& clientStart, GameMath::Vector3& clientTarget)
{
	if (unit.GetPosVector().GetDistance(clientStart) > 0.5f)
	{
		return { false, unit.GetPosVector() };
	}

	int32 gx, gz;
	if (!WorldToGrid(grid, clientTarget, gx, gz))
	{
		return { false, unit.GetPosVector() };
	}

	GridCell& cell = _grids.At(gx, gz);
	if (!cell.walkable)
	{
		return { false, unit.GetPosVector() };
	}

	// 최대 이동 가능 거리 = 이동 속도 * 허용 시간
	Protocol::StatInfo stat = unit.GetStatInfo();
	float moveSpeed = static_cast<float>(stat.speed());
	float maxDist = moveSpeed * MAX_COMMAND_TIME;
	float dist = unit.GetPosVector().GetDistance(clientTarget);

	if (dist > maxDist)
	{
		clientTarget = unit.GetPosVector() + GameMath::Vector3::GetNormalVector(clientTarget - unit.GetPosVector()) * maxDist;
	}

	return { true, clientTarget };
}

void Navigation::NavigationSystem::PrintGrid(WalkableGrid& grids) const
{
	cout << "[PrintGrid] called\n";
	cout << "width=" << grids.width
		<< " height=" << grids.height
		<< " cells=" << grids.cells.size()
		<< endl;

	for (int z = grids.height - 1; z >= 0; --z) // Unity Z축 보정
	{
		for (int x = 0; x < grids.width; ++x)
		{
			const GridCell& cell = grids.cells[z * grids.width + x];
			cout << (cell.walkable ? '.' : '#');
		}
		cout << '\n';
	}
	cout << "PrintGrid End" << endl;
}

void Navigation::NavigationSystem::PrintPath()
{

}

void Navigation::NavigationSystem::PrintGridSummary(const Navigation::WalkableGrid& grid)
{
	size_t walkableCount = 0;

	for (const auto& cell : grid.cells)
	{
		if (cell.walkable)
			++walkableCount;
	}

	const size_t total = grid.cells.size();
	const float ratio = (total > 0) ? (static_cast<float>(walkableCount) / total * 100.0f) : 0.0f;
	cout << "[NavGrid Summary] "
		<< "W=" << grid.width
		<< " H=" << grid.height
		<< " CellSize=" << grid.cellSize
		<< " Total=" << total
		<< " Walkable=" << walkableCount
		<< " (" << ratio << "%)"
		<< endl;
}

void Navigation::NavigationSystem::PrintSampleCells(const Navigation::WalkableGrid& grid)
{
	int32 samples[][2] =
	{
		{0, 0},
		{grid.width / 2, grid.height / 2},
		{grid.width - 1, grid.height - 1}
	};

	for (auto& s : samples)
	{
		int x = s[0];
		int z = s[1];

		const auto& cell = grid.At(x, z);

		float wx = grid.origin._x + (x + 0.5f) * grid.cellSize;
		float wz = grid.origin._z + (z + 0.5f) * grid.cellSize;

		cout << "[SampleCell] "
			<< "Grid(" << x << "," << z << ") "
			<< "World(" << wx << ", " << wz << ") "
			<< "Walkable=" << cell.walkable
			<< endl;
	}
}

void Navigation::NavigationSystem::VerifyWorldGridInvariant(Navigation::WalkableGrid& grid)
{
	for (int32 z = 0; z < grid.height; ++z)
	{
		for (int32 x = 0; x < grid.width; ++x)
		{
			// 갈 수 없는 곳이라면 굳이 교차검증을 할 필요가 없다.
			if (grid.At(x, z).walkable == 0)
				continue;
			
			// 갈 수 있는 곳이니까 Grid에서 World 좌표로 변환
			GameMath::Vector3 w1;
			if (!GridToWorld(grid, x, z, w1))
			{
				return;
			}
			
			int32 gx, gz;
			bool ok = WorldToGrid(grid, w1, gx, gz);
			if (ok == false || gx != x || gz != z)
			{
				GConsoleLogger->WriteStdErr(Color::RED, L"[NavGrid ERROR] RoundTrip failed (%d,%d)->(%d,%d)\n", x, z, gx, gz);
				ASSERT_CRASH(ok);
				return;
			}
		}
	}
	GConsoleLogger->WriteStdOut(Color::GREEN,	L"[NavGrid] World/Grid round-trip OK\n");
}

void Navigation::NavigationSystem::DebugTestWorldPos(GameMath::Vector3& worldPos, Navigation::WalkableGrid& grid)
{
	int32 x, z;
	if (!WorldToGrid(grid, worldPos, x, z))
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[DebugTestWroldToPos] WorldTo Grid Fail");
		return;
	}

	const GridCell& cell = grid.At(x, z);
	//GameMath::Vector3 center = GridToWorld();

	//GConsoleLogger->WriteStdOut()
}

uint8 Navigation::NavigationSystem::GetLaneId(const WalkableGrid& grid, GameMath::Vector3& worldPos)
{
	int32 x = 0, z = 0;
	if (!WorldToGrid(grid, worldPos, x, z))
		return 0;

	if (x < 0 || x >= grid.width || z < 0 || z >= grid.height)
		return 0;


	int32 idx = z * grid.width + x;
	return grid.cells[idx].laneId;
}

void Navigation::NavigationSystem::DebugCheckLaneRouteCoverage(const Navigation::WalkableGrid& grid, const unordered_map<int32, shared_ptr<Navigation::LaneRoute>> routes, Navigation::NavigationSystem& navSystem)
{
	for (const auto& kv : routes)
	{
		int32 laneId = kv.first;
		auto route = kv.second;
		if (!route || route->waypoints.empty())
			continue;

		uint8 laneByte = static_cast<uint8>(laneId);

		for (size_t i = 0; i < route->waypoints.size(); ++i)
		{
			const auto& wp = route->waypoints[i];

			int32 gx = 0, gz = 0;
			if (!navSystem.WorldToGrid(grid, wp._x, wp._z, gx, gz))
			{
				GConsoleLogger->WriteStdOut(Color::RED,
					L"[DebugLaneRoute] laneId=%d wp[%zu] WorldToGrid failed (%.2f, %.2f)\n",
					laneId, i, wp._x, wp._z);
				continue;
			}

			if (gx < 0 || gz < 0 || gx >= grid.width || gz >= grid.height)
			{
				GConsoleLogger->WriteStdOut(Color::RED,
					L"[DebugLaneRoute] laneId=%d wp[%zu] mapped out of grid (%d,%d)\n",
					laneId, i, gx, gz);
				continue;
			}

			const auto& cell = grid.At(gx, gz);

			GConsoleLogger->WriteStdOut(Color::YELLOW,
				L"[DebugLaneRoute] laneId=%d wp[%d] world=(%.3f, %.3f) -> grid(%d,%d) laneId=%d\n",
				laneId,
				(int)i,
				wp._x,
				wp._z,
				gx,
				gz,
				cell.laneId);

			if (cell.laneId != laneByte)
			{
				for (int dz = -1; dz <= 1; ++dz)
				{
					for (int dx = -1; dx <= 1; ++dx)
					{
						int nx = gx + dx;
						int nz = gz + dz;

						if (nx < 0 || nz < 0 || nx >= grid.width || nz >= grid.height)
							continue;

						auto& ncell = grid.At(nx, nz);

						GConsoleLogger->WriteStdOut(
							Color::WHITE,
							L"    neighbor(%d,%d) laneId=%d\n",
							nx,
							nz,
							ncell.laneId);
					}
				}

				GConsoleLogger->WriteStdOut(Color::RED,
					L"[DebugLaneRoute] MISMATCH: laneId=%d wp[%zu] grid(%d,%d) laneId=%d (expected %d)\n",
					laneId, i, gx, gz, cell.laneId, laneByte);
			}
		}
	}
}

bool Navigation::NavigationSystem::WorldToGridImpl(const WalkableGrid& grid, float worldX, float worldZ, int32& X, int32& Z)
{
	float localX = (worldX - grid.origin._x) / grid.cellSize;
	float localZ = (worldZ - grid.origin._z) / grid.cellSize;
	int32 x = static_cast<int32>(std::floor(localX));
	int32 z = static_cast<int32>(std::floor(localZ));

	if (x < 0 || z < 0 || x >= grid.width || z >= grid.height)
		return false;

	X = x;
	Z = z;
	return true;
}

Navigation::WalkableGrid::WalkableGrid()
{
}

Navigation::WalkableGrid::~WalkableGrid()
{
}
