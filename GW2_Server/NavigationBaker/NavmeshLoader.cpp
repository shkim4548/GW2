#include "NavmeshLoader.h"
#include "Types.h"
#include <fstream>
#include <sstream>
#include <filesystem>
#include "ToolMath.h"
#include <nlohmann/json.hpp>
using namespace std;
namespace fs = std::filesystem;

void Navigation::NavmeshLoader::LoadObjFile(const char* filePath, OBJ_CollisionMesh& mesh)
{
    ifstream file(filePath);

    if (!file.is_open())
    {
		fs::path p(filePath);
		cout << fs::absolute(p) << endl;
		cout << fs::current_path() << endl;
		//cout << "is Open Fail" << endl;
        return;
    }

    string line;
    while (getline(file, line))
    {
        stringstream ss(line);
        string tag;
        ss >> tag;

        if (tag == "v")
        {
            GameMath::Vector3 v;
            ss >> v._x >> v._y >> v._z;
            mesh.vertices.push_back(v);
        }
        else if (tag == "f")
        {
            // OBJ는 보통 f v/vt/vn 형식이므로 추가 파싱 필요
            string a, b, c;
            ss >> a >> b >> c;

            auto ToIndex = [&](const string& s)
                {
                    // "12/1/3" → 12 추출
                    stringstream sss(s);
                    int32 idx;
                    sss >> idx;
                    return idx - 1; // OBJ는 1-based
                };

            /*Triangle tri;
            tri.v1 = mesh.vertices[ToIndex(a)];
            tri.v2 = mesh.vertices[ToIndex(b)];
            tri.v3 = mesh.vertices[ToIndex(c)];
            mesh.indices.push_back(tri);*/
			mesh.indices.push_back(ToIndex(a));
			mesh.indices.push_back(ToIndex(b));
			mesh.indices.push_back(ToIndex(c));
        }
    }

    cout << "OBJ Loading Complete. Vertices: "
        << mesh.vertices.size() << ", Faces: "
        << mesh.indices.size() << endl;
}

void Navigation::NavmeshLoader::SaveGridToJsonFile(const WalkableGrid& grid, const string& path)
{
	std::ofstream out(path, std::ios::out | std::ios::trunc);
	if (!out.is_open())
	{
		std::cout << "[SaveGridToJson] Failed to open file\n";
		return;
	}

	out << "{\n";
	out << "  \"grid\": {\n";
	out << "    \"width\": " << grid.width << ",\n";
	out << "    \"height\": " << grid.height << ",\n";
	out << "    \"cellSize\": " << grid.cellSize << ",\n";

	out << "    \"origin\": { "
		<< "\"x\": " << grid.origin._x << ", "
		<< "\"y\": " << grid.origin._y << ", "
		<< "\"z\": " << grid.origin._z << " },\n";

	out << "    \"cells\": [\n";

	for (size_t i = 0; i < grid.cells.size(); ++i)
	{
		const GridCell& c = grid.cells[i];

		out << "      {\n";
		out << "        \"x\": " << c.x << ",\n";
		out << "        \"z\": " << c.z << ",\n";
		out << "        \"walkable\": " << (c.walkable ? "true" : "false") << ",\n";
		out << "        \"height\": " << c.height << ",\n";

		out << "        \"neighbors\": ["
			<< c.neighbors[0] << ", "
			<< c.neighbors[1] << ", "
			<< c.neighbors[2] << ", "
			<< c.neighbors[3] << "]\n";

		out << "      }";

		if (i + 1 < grid.cells.size())
			out << ",";

		out << "\n";
	}

	out << "    ]\n";
	out << "  }\n";
	out << "}\n";

	out.close();
}

bool Navigation::NavmeshLoader::SaveNavmeshCache(const vector<Triangle>& triangles, const Navigation::WalkableGrid& grid, const string& path)
{
	ofstream out(path, ios::binary | ios::trunc);
	if (!out.is_open())
		return false;

	NavmeshCacheHeader header;
	header.magic = 0x4E415643; // 'NAVC'
	header.version = 1;
	header.triangleCount = (uint32)triangles.size();
	header.cellCount = (uint32)grid.cells.size();

	out.write((char*)&header, sizeof(header));
	out.write((char*)triangles.data(),
		sizeof(Triangle) * triangles.size());
	out.write((char*)grid.cells.data(),
		sizeof(GridCell) * grid.cells.size());

	out.close();
	return true;
}

json Navigation::NavmeshLoader::ToJson(const GameMath::Vector3& v)
{
	return json::array({ v._x, v._y, v._z });
}

GameMath::Vector3 Navigation::NavmeshLoader::FromJsonVector3(const json& j)
{
	return { j[0].get<float>(),	j[1].get<float>(), j[2].get<float>() };
}

void Navigation::NavmeshLoader::TriangleToJson(ostream& out, const Triangle& t)
{
	out << "{";

	out << "\"v1\":";
	WriteVec3(out, t.v1);
	out << ",";

	out << "\"v2\":";
	WriteVec3(out, t.v2);
	out << ",";

	out << "\"v3\":";
	WriteVec3(out, t.v3);
	out << ",";

	out << "\"normal\":";
	WriteVec3(out, t.normal);

	out << "}";
}

void Navigation::NavmeshLoader::GridCellToJson(ostream& out, const Navigation::GridCell& c)
{
	out << "{";
	out << "\"x\":" << c.x << ",";
	out << "\"z\":" << c.z << ",";
	out << "\"walkable\":" << (c.walkable ? "true" : "false") << ",";
	out << "\"height\":" << c.height << ",";
	out << "\"neighbors\":["
		<< (c.neighbors[0] ? 1 : 0) << ","
		<< (c.neighbors[1] ? 1 : 0) << ","
		<< (c.neighbors[2] ? 1 : 0) << ","
		<< (c.neighbors[3] ? 1 : 0)
		<< "]";
	out << "}";
}

void Navigation::NavmeshLoader::GridToJson(ostream& out, const Navigation::WalkableGrid& grid)
{
	out << "\"width\":" << grid.width << ",";
	out << "\"height\":" << grid.height << ",";
	out << "\"cellSize\":" << grid.cellSize << ",";
	out << "\"origin\":";
	WriteVec3(out, grid.origin);
	out << ",\"cells\":[\n";

	for (size_t i = 0; i < grid.cells.size(); ++i)
	{
		GridCellToJson(out, grid.cells[i]);
		if (i + 1 < grid.cells.size())
			out << ",\n";
	}

	out << "]\n";
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
	if (!RaycastWorld(ray, FLT_MAX, /*cullBackFace=*/false, hit))
	{
		return false;
	}

	outY = hit.position._y;
	return true;
}

int32 Navigation::NavigationSystem::GetGroundVertical()
{
	return _gridCols;
}

int32 Navigation::NavigationSystem::GetGroundWidth()
{
	return _gridRows;
}


GameMath::Vector3 Navigation::NavigationSystem::GetGridOrigin()
{
	// Grid (0,0)의 월드 좌표 기준점 (셀의 "좌하단")
	return GameMath::Vector3(
		_mapMinX,
		0.0f,      // Y는 Grid 기준점이므로 0 또는 무시
		_mapMinZ
	);
}

vector<Triangle>& Navigation::NavigationSystem::GetAllTriangles()
{
	return _allTriangles;
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

bool Navigation::NavigationSystem::RaycastWorld(const Ray& ray, float maxDistance, bool cullBackFace, RaycastHit& outHit)
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

void Navigation::NavigationSystem::PrintGrid() const
{
	for (int32 z = _gridCols - 1; z >= 0; --z)
	{
		for (int32 x = 0; x < _gridRows; ++x)
		{
			const GridCell& cell = _cells[z * _gridRows + x];
			cout << cell.walkable ? "[ ]" : "[X]";
		}
		cout << '\n';
	}
}

void Navigation::NavigationSystem::PrintPath()
{

}

