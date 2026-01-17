#include "pch.h"
#include "Object.h"
#include "NavigationSystem.h"

Object::Object()
{
}

Object::~Object()
{
}

void Object::SetPath(const NavPath& path)
{
	if (path.empty())
	{
		_isMoving = false;
		_path.clear();
		_pathIndex = 0;
		return;
	}

	_path = path;
	_pathIndex = 0;
	_isMoving = true;
}

void Object::UpdateMovement(float deltaTime)
{
if (!_isMoving || _pathIndex >= _path.size())
		return;

	const float speed = GetStatInfo().speed();
	const float maxMove = speed * deltaTime;

	Navigation::GridCell* cell = _path[_pathIndex];

	GameMath::Vector3 targetPos =
		_navigationSystem.lock()->GridToWorld(
			_navigationSystem.lock()->GetGridCells(),
			cell->x,
			cell->z
		);

	float dist = _posVector.GetDistance(targetPos);

	if (dist <= maxMove)
	{
		_posVector = targetPos;
		_pathIndex++;

		if (_pathIndex >= _path.size())
			_isMoving = false;
	}
	else
	{
		GameMath::Vector3 dir = GameMath::Vector3::GetNormalVector((targetPos - _posVector));
		_posVector = _posVector + (dir * maxMove);
	}
}
