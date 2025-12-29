#pragma once
#include <cmath>
#include "Types.h"
#include <iostream>
#include <vector>
#include <optional>
#define OUT 
using namespace std;

namespace GameMath {
	/*--------------------
			Vector3
	---------------------*/

	struct Vector3
	{
	public:
		Vector3() : _x(0), _y(0), _z(0) {
		};
		Vector3(float x, float y, float z) : _x(x), _y(y), _z(z) {
		};

		// 연산자 및 필요한 함수
	public:
		Vector3 operator+(const Vector3& other) const {
			return Vector3(_x + other._x, _y + other._y, _z + other._z);
		}
		Vector3 operator-(const Vector3& other) const {
			return Vector3(_x - other._x, _y - other._y, _z - other._z);
		}
		Vector3 operator*(const Vector3& other) const {
			return Vector3(_x * other._x, _y * other._y, _z * other._z);
		}
		Vector3 operator*(const float& other) const {
			return Vector3(_x * other, _y * other, _z * other);
		}
		Vector3 operator/(const Vector3& other) const {
			return Vector3(_x / other._x, _y / other._y, _z / other._z);
		}
		Vector3 operator/(const float& other) const {
			return Vector3(_x / other, _y / other, _z / other);
		}
		bool operator==(const Vector3& other) const {
			return _x == other._x && _z == other._z;
		}

	public:
		static float GetMagnitude(Vector3 v);
		static Vector3 GetNormalVector(Vector3 v);
		float GetDistance(const Vector3& other) const {
			return (float)std::sqrt((_x - other._x) * (_x - other._x) + (_z - other._z) * (_z - other._z));
		}
		//Protocol::PosInfo GetPositionFromVector(Vector3 v, float yaw = 0);
		//static Vector3 GetVectorFromPosition(Protocol::PosInfo* pos);
		static Vector3 YawToDirectionVector(float yaw);
		static float DirectionVectorToYaw(Vector3 dir);
		static Vector3 Lerp(Vector3 start, Vector3 end, float t);
		Vector3 Cross(const Vector3& o) const;
		float Dot(const Vector3& o) const {
			return _x * o._x + _y * o._y + _z * o._z;
		}
		float Length() const;

	public:
		// 출력용 함수
		static void Print(Vector3 v) {
			std::cout << "Vector3(" << v._x << ", " << v._y << ", " << v._z << ")" << std::endl;
		}
	public:
		float _x;
		float _y;
		float _z;
	};

}

struct Triangle
{
	GameMath::Vector3 v1, v2, v3;
	GameMath::Vector3 normal;
};

struct Ray
{
	GameMath::Vector3 origin;
	GameMath::Vector3 dir;
};

struct RaycastHit
{
	float t;
	GameMath::Vector3 position;
	GameMath::Vector3 normal;
	const Triangle* triangle;
};

struct HitResult
{
	float t;			// 레이 파라미터
	float u, v;			// 바리센트릭 좌표
	GameMath::Vector3 position;	// 교차 지점
	GameMath::Vector3 normal;		// 삼각형 법선
};

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
}