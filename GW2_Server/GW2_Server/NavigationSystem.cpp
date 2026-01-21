#include "pch.h"
#include "GameLogic.h"
#include "Object.h"
#include "NavigationSystem.h"

#undef min
#undef max

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
	if (!RaycastWorld(
		ray,
		length,
		/*cullBackFace=*/true,
		hit))
	{
		return true;
	}

	// 바닥 판정: 법선이 위를 향하면 통과
	if (hit.normal._y > 0.7f)
		return true;

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
	// obj 파일 기반의 Grid의 핵심부
	for (int32 z = 0; z < grid.height; ++z)
	{
		for (int32 x = 0; x < grid.width; ++x)
		{
			GridCell& cell = grid.At(x, z);
			if (!cell.walkable == false)
				continue;

			GameMath::Vector3 from = GridToWorld(grid, x, z);

			// North
			if (z + 1 < grid.height && grid.At(x, z + 1).walkable)
			{
				GameMath::Vector3 to = GridToWorld(grid, x, z + 1);
				if (CanMoveStraight(from, to))
					cell.neighbors[DIR_NORTH] = true;
			}

			// East
			if (x + 1 < grid.width && grid.At(x + 1, z).walkable)
			{
				GameMath::Vector3 to = GridToWorld(grid, x, z + 1);
				if (CanMoveStraight(from, to))
				{
					cell.neighbors[DIR_EAST] = true;
				}
			}

			// South
			if (z > 0 && grid.At(x, z - 1).walkable)
			{
				GameMath::Vector3 to = GridToWorld(grid, x + 1, z);
				if (CanMoveStraight(from, to))
				{
					cell.neighbors[DIR_SOUTH] = true;
				}
			}

			// West
			if (x > 0 && grid.At(x - 1, z).walkable)
			{
				GameMath::Vector3 to = GridToWorld(grid, x - 1, z);
				if (CanMoveStraight(from, to))
				{
					cell.neighbors[DIR_WEST] = true;
				}
			}
		}
	}
}

GameMath::Vector3 Navigation::NavigationSystem::GridToWorld(WalkableGrid& grid, int32 x, int32 z)
{
	float worldX = grid.origin._x + (x + 0.5f) * grid.cellSize;
	float worldZ = grid.origin._z + (z + 0.5f) * grid.cellSize;
	float y = grid.At(x, z).height;

	return GameMath::Vector3(worldX, y, worldZ);
}

bool Navigation::NavigationSystem::WorldToGrid(GameMath::Vector3& worldPos, int32& OUT x, int32& OUT z)
{
	return WorldToGridImpl(_grids, worldPos._x, worldPos._z, x, z);
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
			_mapMaxZ = std::max(_mapMinZ, v._z);
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

void Navigation::NavigationSystem::Init(WalkableGrid&& grid)
{
	_grids = move(grid);
}

bool Navigation::NavigationSystem::FindPath(int32 startX, int32 startZ, int32 endX, int32 endZ, vector<GridCell*>& outPath)
{
	outPath.clear();

	if (!_grids.At(startX, startZ).walkable ||
		!_grids.At(endX, endZ).walkable)
		return false;

	unordered_map<NodeKey, AStarNode*, NodeKeyHash> allNodes;
	unordered_set<NodeKey, NodeKeyHash> closed;

	auto cmp = [](AStarNode* a, AStarNode* b)
		{
			return a->f > b->f;
		};

	priority_queue<AStarNode*, vector<AStarNode*>, decltype(cmp)> open(cmp);

	AStarNode* start = new AStarNode{ startX, startZ };
	start->g = 0;
	start->h = Heuristic(startX, startZ, endX, endZ);
	start->f = start->h;

	open.push(start);
	allNodes[{startX, startZ}] = start;

	while (!open.empty())
	{
		AStarNode* cur = open.top();
		open.pop();

		NodeKey ck{ cur->x, cur->z };
		if (closed.count(ck))
			continue;

		closed.insert(ck);

		if (cur->x == endX && cur->z == endZ)
		{
			// 경로 복원
			while (cur)
			{
				outPath.push_back(&_grids.At(cur->x, cur->z));
				cur = cur->parent;
			}
			reverse(outPath.begin(), outPath.end());
			return true;
		}

		GridCell& cell = _grids.At(cur->x, cur->z);

		static const int dx[4] = { 0, 1, 0, -1 };
		static const int dz[4] = { -1, 0, 1, 0 };

		for (int dir = 0; dir < 4; dir++)
		{
			if (!cell.neighbors[dir])
				continue;

			int nx = cur->x + dx[dir];
			int nz = cur->z + dz[dir];

			NodeKey nk{ nx, nz };
			if (closed.count(nk))
				continue;

			GridCell& nextCell = _grids.At(nx, nz);

			float cost = cur->g + 1.0f;
			// 선택: 높이 비용
			cost += fabs(nextCell.height - cell.height) * 0.2f;

			AStarNode*& node = allNodes[nk];
			if (!node)
			{
				node = new AStarNode{ nx, nz };
				node->g = cost;
				node->h = Heuristic(nx, nz, endX, endZ);
				node->f = node->g + node->h;
				node->parent = cur;
				open.push(node);
			}
			else if (cost < node->g)
			{
				node->g = cost;
				node->f = cost + node->h;
				node->parent = cur;
				open.push(node);
			}
		}
	}

	return false;
}

Navigation::MoveValidationResult Navigation::NavigationSystem::ValidateMove(const Object& unit, GameMath::Vector3& clientStart, GameMath::Vector3& clientTarget)
{
	if (unit.GetPosVector().GetDistance(clientStart) > 0.5f)
	{
		return { false, unit.GetPosVector() };
	}

	int32 gx, gz;
	if (!WorldToGrid(clientTarget, gx, gz))
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

bool Navigation::NavigationSystem::WorldToGridImpl(const WalkableGrid& grid, float worldX, float worldZ, int32& x, int32 z)
{
	float localX = worldX - grid.origin._x;
	float localZ = worldZ - grid.origin._z;

	if (localX < 0.0f || localZ < 0.0f)
		return false;

	int32 gx = static_cast<int32>(localX / grid.cellSize);
	int32 gz = static_cast<int32>(localZ / grid.cellSize);

	if (gx < 0 || gz < 0 || gx >= grid.width || gz >= grid.height)
		return false;

	x = gx;
	z = gz;
	return true;
}

