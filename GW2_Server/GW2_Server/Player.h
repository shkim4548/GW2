#pragma once
class Player
{
public:
	int32 GetPlayerId() { return _playerId; }

public:
	int64 _playerId = 0;
	string name;
	Protocol::PlayerType type = Protocol::PLAYER_TYPE_NONE;
	weak_ptr<GameSession> _ownerSession;	// Cycle
};

