#pragma once
#include "GameLogic.h"

namespace GameMath{ struct Vector3; }

class Object
{
public:
	Object();
	virtual ~Object();

	int32 GetObjectId() { return _objectId; }
	Protocol::PosInfo GetPosInfo() { return _pos; }
	GameMath::Vector3 GetPosVector() const { return _posVector; }
	Protocol::StatInfo GetStatInfo() const { return _statInfo; }

	void SetObjectId(int64 id) { _objectId = id; }
public:
	

protected:
	int64 _objectId = 0;
	Protocol::PosInfo _pos;
	Protocol::StatInfo _statInfo;
	GameMath::Vector3 _posVector;
};

