#include "pch.h"
#include "GameLogic.h"
/*------------
	Vector3
-------------*/

float Vector3::GetMagnitude(Vector3 v)
{
	return (float)std::sqrt(v._x * v._x + v._y * v._y + v._z * v._z);
}

Vector3 Vector3::GetNormalVector(Vector3 v)
{
	float norm = GetMagnitude(v);
	if (norm != 0)
		return Vector3(v._x / norm, v._y / norm, v._z / norm);

	return Vector3();
}

Protocol::PosInfo Vector3::GetPositionFromVector(Vector3 v, float yaw)
{
	Protocol::PosInfo ret;
	ret.set_x(v._x);
	ret.set_y(v._y);
	ret.set_z(v._z);
	return ret;
}

Vector3 Vector3::GetVectorFromPosition(Protocol::PosInfo* pos)
{
	Vector3 ret = Vector3(pos->x(), pos->y(), pos->z());
	return ret;
}

Vector3 Vector3::YawToDirectionVector(float yaw)
{
	Vector3 ret;
	float radians = yaw / 100 * M_PI;

	float x = sin(radians);
	float z = cos(radians);

	ret = Vector3(x, 0, z);
	return ret;
}

float Vector3::DirectionVectorToYaw(Vector3 dir)
{
	float x = dir._x;
	float z = dir._z;

	float magnitude = GetMagnitude(Vector3(x, 0, z));
	x /= magnitude;
	z /= magnitude;

	float yaw = (float)atan2(x, z) * (180 / (float)M_PI);
	return yaw;
}

Vector3 Vector3::Lerp(Vector3 start, Vector3 end, float t)
{
	t = clamp(t, 0.0f, 0.1f);
	return start + (end - start) * t;
}

Vector3 Vector3::Cross(const Vector3& o) const
{
	return Vector3(_y * o._z - _z * o._y,
		_z * o._x - _x * o._z,
		_x * o._y - _y * o._x);
}

float Vector3::Length() const
{
	return std::sqrt(_x * _x + _y * _y + _z * _z);
}