#include "pch.h"
#include "GameLogic.h"
/*------------
	Vector3
-------------*/

float GameMath::Vector3::GetMagnitude(Vector3 v)
{
	return (float)std::sqrt(v._x * v._x + v._y * v._y + v._z * v._z);
}

GameMath::Vector3 GameMath::Vector3::GetNormalVector(Vector3 v)
{
	float norm = GetMagnitude(v);
	if (norm != 0)
		return Vector3(v._x / norm, v._y / norm, v._z / norm);

	return Vector3();
}

GameMath::Vector3 GameMath::Vector3::Normalized()
{
	float len = Length();
	if (len < 1e-6f)
		return Vector3{ 0,0,0 };

	return Vector3{ _x / len, _y / len, _z / len };
}

Protocol::PosInfo GameMath::Vector3::GetPositionFromVector(Vector3 v, float yaw)
{
	Protocol::PosInfo ret;
	ret.set_x(v._x);
	ret.set_y(v._y);
	ret.set_z(v._z);
	return ret;
}

GameMath::Vector3 GameMath::Vector3::GetVectorFromPosition(Protocol::PosInfo* pos)
{
	GameMath::Vector3 ret = Vector3(pos->x(), pos->y(), pos->z());
	return ret;
}

GameMath::Vector3 GameMath::Vector3::YawToDirectionVector(float yaw)
{
	GameMath::Vector3 ret;
	float radians = yaw / 100 * M_PI;

	float x = sin(radians);
	float z = cos(radians);

	ret = GameMath::Vector3(x, 0, z);
	return ret;
}

float GameMath::Vector3::DirectionVectorToYaw(GameMath::Vector3 dir)
{
	float x = dir._x;
	float z = dir._z;

	float magnitude = GetMagnitude(GameMath::Vector3(x, 0, z));
	x /= magnitude;
	z /= magnitude;

	float yaw = (float)atan2(x, z) * (180 / (float)M_PI);
	return yaw;
}

GameMath::Vector3 GameMath::Vector3::Lerp(Vector3 start, Vector3 end, float t)
{
	t = clamp(t, 0.0f, 1.0f);
	return start + (end - start) * t;
}

GameMath::Vector3 GameMath::Vector3::Cross(const GameMath::Vector3& o) const
{
	return GameMath::Vector3(_y * o._z - _z * o._y,
		_z * o._x - _x * o._z,
		_x * o._y - _y * o._x);
}

float GameMath::Vector3::Length() const
{
	return std::sqrt(_x * _x + _y * _y + _z * _z);
}

GameMath::Vector2 GameMath::Vector2::Normalized2D()
{
	float len = Length2D();
	if (len < 1e-6f)
		return GameMath::Vector2{ 0, 0 };

	return GameMath::Vector2{ _x / len, _z / len };
}

float GameMath::Vector2::Length2D()
{
	return std::sqrt(_x * _x  + _z * _z);
}
