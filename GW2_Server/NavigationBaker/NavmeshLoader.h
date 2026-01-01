#pragma once
#include <vector>
#include "ToolMath.h"

struct OBJ_CollisionMesh
{
	vector<GameMath::Vector3> Vertices;
	vector<Triangle> Triangles;
};

class NavmeshLoader
{
public:
	void LoadObjFile(const char* filePath, OBJ_CollisionMesh& mesh);
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

}