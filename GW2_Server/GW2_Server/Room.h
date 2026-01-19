#pragma once
#include "Job.h"
#include "JobQueue.h"
#include "Protocol.pb.h"

namespace Navigation { class NavigationSystem; }

class Room : public JobQueue
{
public:
	Room();
	virtual ~Room();

	bool Enter(PlayerRef player);
	void Leave(int32 playerId);
	void Broadcast(SendBufferRef sendBuffer, int32 exceptId = -1);

	void InitNavigation();

	void SetRoomId(int32 roomId) { _roomId = roomId; }
	int32 GetRoomId() { return _roomId; }
	void SetRoomName(string roomName) { _roomName = roomName; }
	string GetRoomName() { return _roomName; }

	weak_ptr<Player> GetPlayerById(int32 id) { return weak_ptr<Player>(_players[id]); }

public:
	// Handlers
	bool HandleEnterPlayer(PlayerRef player);
	void HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt);
	bool HandleMovePlayer(PlayerRef player, Protocol::C_MOVE movePkt);

private:

private:
	unordered_map<int32, PlayerRef> _players;
	int32 _roomId;
	string _roomName;
	weak_ptr<Navigation::NavigationSystem> _navigationSystem;
};

extern shared_ptr<Room> GRoom;

