#pragma once
#include "Object.h"

class Player : public Object
{
public:
	Player();
	virtual ~Player();

	void InitPlayer();

	void SetPlayerId(int32 id) { _objectId = id; }
	void SetSession(GameSessionRef session) { _ownerSession = session; }
	int64 GetPlayerId() { return _objectId; }
	//weak_ptr<GameSession> GetSession() { return _ownerSession.load(); }


public:
	string name;
	Protocol::PlayerType type = Protocol::PLAYER_TYPE_NONE;
	weak_ptr<GameSession> _ownerSession;	// Cycle
};

