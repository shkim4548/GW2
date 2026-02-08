#pragma once
#include "Object.h"

class Player : public Object
{
public:
	Player();
	virtual ~Player();

	void InitPlayer();

	void SetPlayerId(int32 id) { _objectId = id; }
	void SetSession(GameSessionRef session) { _session = session; }
	int64 GetPlayerId() { return _objectId; }
	weak_ptr<GameSession> GetSession() { return _session.load(); }

	// Contents
	void StartMove(const GameMath::Vector3& startPos, const GameMath::Vector3& targetPos, const vector<Navigation::GridCell*>& path, int32 clientTime);

private:
	// Server System
	string name;
	Protocol::PlayerType type = Protocol::PLAYER_TYPE_NONE;
	atomic<weak_ptr<GameSession>> _session;
	int32 roomId;

	// Contents
	Protocol::PosInfo pos;
	GameMath::Vector3 posVector;
	vector<GameMath::Vector3> path;
	int32 currentPathIndex;
	bool _isMoving = false;
};

