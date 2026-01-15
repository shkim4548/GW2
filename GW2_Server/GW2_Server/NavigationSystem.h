#pragma once
//#include <memory>
#include "GameLogic.h"

struct GameMath::Vector3;
struct Triangle;
class Object;

namespace Navigation
{
	/*-------------
		GridCell
	---------------*/
	struct FileHeader
	{
		uint32 magic;	// NRGD
		uint16 version;	// 1
	};

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

	/*----------------
		A Star Node
	------------------*/
	struct AStarNode
	{
		int32 x;
		int32 z;

		float g;
		float h;
		float f;

		AStarNode* parent = nullptr;
	};

	struct NodeKey
	{
		int32 x;
		int32 z;
		bool operator==(const NodeKey& o) const { return x == o.x && z == o.z; }
	};

	struct NodeKeyHash
	{
		size_t operator()(const NodeKey& k) const { return (k.x << 16) ^ k.z; }
	};

	/*------------------
		MoveValidation
	--------------------*/
	struct MoveValidationResult
	{
		bool accepted;
		GameMath::Vector3 approvedTarget;
	};

	/*---------------------
		Navigation Logic
	-----------------------*/
	class NavigationSystem
	{
	public:
		void Build(const vector<GameMath::Vector3> vertices, const vector<int32>& indices);
		bool GetGroundHeight(float x, float z, float& OUT outY);
		bool CanMoveStraight(GameMath::Vector3 start, GameMath::Vector3 end);
		// 뮐러 트럼보 교차 알고리즘
		optional<HitResult> RayIntersects(const Ray& ray, const GameMath::Vector3& v1, const GameMath::Vector3& v2, const GameMath::Vector3& v3, bool cullBackFace = false);
		bool RaycastWorld(const Ray& ray, float maxDistance, bool cullBackFace, RaycastHit& outHit);

		// Grid Logic
		void BuildWalkableGrid(WalkableGrid& grid, int32 width, int32 height, float cellSize, GameMath::Vector3 origin);
		void BuildCells(WalkableGrid& grid);
		void BuildConnections(WalkableGrid& grid);
		GameMath::Vector3 GridToWorld(WalkableGrid& grid, int32 x, int32 z);
		bool WorldToGrid(GameMath::Vector3& worldPos, int32& OUT x, int32& OUT z);
		void BuildGrid(float cellSize);

		vector<Triangle>& GetAllTriangles() { return _allTriangles; }
		WalkableGrid GetGridCells() { return _grids; }
		int32 GetGroundVertical() { return _gridCols; }
		int32 GetGroundWidth() { return _gridRows; }
		GameMath::Vector3 GetGridOrigin() { return GameMath::Vector3(_mapMinX, 0.0f, _mapMinZ); }

		void InitNavmesh(vector<Triangle>&& triangles, WalkableGrid&& grid);

		// A Star
		void Init(WalkableGrid&& grid);
		bool FindPath(int32 startX, int32 startZ, int32 endX, int32 endZ, vector<GridCell*>& outPath);
		inline float Heuristic(int32 x1, int32 z1, int32 x2, int32 z2) { return abs(x1 - x2) + abs(z1 - z2); }

		// 위치 보정
		MoveValidationResult ValidateMove(const Object& unit, GameMath::Vector3& clientStart, GameMath::Vector3& clientTarget);

		// DEBUG
		void PrintGrid() const;
		void PrintPath();

	private:
		static bool WorldToGridImpl(const WalkableGrid& grid, float worldX, float worldZ, int32& OUT x, int32 OUT z);

	private:
		vector<Triangle> _allTriangles;
		vector<GridCell> _cells;
		WalkableGrid _grids;

		int32 _gridRows = 0;
		int32 _gridCols = 0;
		float _cellSize = 5.0f;
		float _mapMinX;
		float _mapMinZ;
		float _mapMaxX;
		float _mapMaxZ;
	};
	
}