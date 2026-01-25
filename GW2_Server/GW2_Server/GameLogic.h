#pragma once
#define M_PI 3.14159265358979323846

namespace GameMath
{


	/*--------------------
		Quaternion
	---------------------*/

	struct Quaternion
	{
	public:
		Quaternion() : _x(0), _y(0), _z(0), _w(0) {
		}
		Quaternion(float x, float y, float z, float w) : _x(x), _y(y), _z(z), _w(w) {
		};

	public:
		static Quaternion Euler(float pitch, float yaw, float roll)
		{
			return Quaternion();
		}

	public:
		float _x;
		float _y;
		float _z;
		float _w;
	};

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
		Vector3 Normalized();
		float GetDistance(const Vector3& other) const {
			return (float)std::sqrt((_x - other._x) * (_x - other._x) + (_z - other._z) * (_z - other._z));
		}
		Protocol::PosInfo GetPositionFromVector(Vector3 v, float yaw = 0);
		static Vector3 GetVectorFromPosition(Protocol::PosInfo* pos);
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

/*-----------------
	Raycasting
-------------------*/

namespace GameMath { struct Vector3; }

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

class GameLogic
{
	
};