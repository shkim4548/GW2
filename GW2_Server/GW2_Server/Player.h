#pragma once
#include "Object.h"

class Player : public Object
{
public:

public:
	string name;
	Protocol::PlayerType type = Protocol::PLAYER_TYPE_NONE;
	weak_ptr<GameSession> _ownerSession;	// Cycle
};

