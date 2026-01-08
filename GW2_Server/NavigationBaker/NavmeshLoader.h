#pragma once
#include <vector>
#include <memory>
#include "ToolMath.h"

namespace Navigation { class WalkableGrid; }

struct OBJ_CollisionMesh
{
	vector<GameMath::Vector3> vertices;
	vector<int32> indices;
};

struct NavmeshCacheHeader
{
	uint32 magic;   // 파일 식별자
	uint32 version; // 포맷 버전
	uint32 triangleCount;
	uint32 cellCount;
};


struct GameMath::Vector3;
struct Triangle;

namespace Navigation
{
	/*-------------
		GridCell
	---------------*/
	struct GridCell
	{
		bool walkable = false;
		float height = 0.0f;

		// 4방향 탐색
		bool neighbors[4] = { false, false, false, false };

		// grid index
		int32 x;
		int32 z;
	};

	enum Dir
	{
		DIR_NORTH = 0,
		DIR_EAST = 1,
		DIR_SOUTH = 2,
		DIR_WEST = 3,
	};

	class WalkableGrid
	{
	public:
		int32 width;
		int32 height;
		float cellSize;
		GameMath::Vector3 origin;

		vector<GridCell> cells;
		GridCell& At(int32 x, int32 z)
		{
			return cells[z * width + x];
		}

	};

	/*---------------------
		Navigation Logic
	-----------------------*/
	class NavigationSystem
	{
	public:
		void Build(const vector<GameMath::Vector3> vertices, const vector<int32>& indices);
		bool GetGroundHeight(float x, float z, float& OUT outY);
		int32 GetGroundVertical();
		int32 GetGroundWidth();
		GameMath::Vector3 GetGridOrigin();
		vector<Triangle>& GetAllTriangles();
		bool CanMoveStraight(GameMath::Vector3 start, GameMath::Vector3 end);
		// 뮐러 트럼보 교차 알고리즘
		optional<HitResult> RayIntersects(const Ray& ray, const GameMath::Vector3& v1, const GameMath::Vector3& v2, const GameMath::Vector3& v3, bool cullBackFace = false);
		bool RaycastWorld(const Ray& ray, float maxDistance, bool cullBackFace, RaycastHit& outHit);

		// Grid Logic
		void BuildWalkableGrid(WalkableGrid& grid, int32 width, int32 height, float cellSize, GameMath::Vector3 origin);
		void BuildCells(WalkableGrid& grid);
		void BuildConnections(WalkableGrid& grid);
		GameMath::Vector3 GridToWorld(WalkableGrid& grid, int32 x, int32 z);
		void BuildGrid(float cellSize);

		// A Star

		// DEBUG
		void PrintGrid() const;
		void PrintPath();

	private:
		vector<Triangle> _allTriangles;
		vector<GridCell> _cells;

		int32 _gridRows = 0;
		int32 _gridCols = 0;
		float _cellSize = 5.0f;
		float _mapMinX;
		float _mapMinZ;
		float _mapMaxX;
		float _mapMaxZ;
	};

	class NavmeshMaker
	{

	};


	class NavmeshLoader
	{
	public:
		void LoadObjFile(const char* filePath, OBJ_CollisionMesh& mesh);
		void SaveGridToJsonFile(const WalkableGrid& grid, const string& path);
		bool SaveNavmeshCache(const vector<Triangle>& triangles, const Navigation::WalkableGrid& grid, const string& path);

	public:
		// JSON 헬퍼 함수
		static json ToJson(const GameMath::Vector3& v);
		static GameMath::Vector3 FromJsonVector3(const json& j);
		void TriangleToJson(ostream& out, const Triangle& t);
		void GridCellToJson(ostream& out, const Navigation::GridCell& c);
		void GridToJson(ostream& out, const Navigation::WalkableGrid& grid);
		
		inline void WriteVec3(std::ostream& out, const GameMath::Vector3& v)
		{
			out << "{"
				<< "\"x\":" << v._x << ","
				<< "\"y\":" << v._y << ","
				<< "\"z\":" << v._z
				<< "}";
		}
	};
}