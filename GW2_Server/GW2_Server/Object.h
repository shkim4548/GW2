#pragma once
class Object
{
public:
	int32 GetPlayerId() { return _objectId; }
protected:
	int64 _objectId = 0;
};

