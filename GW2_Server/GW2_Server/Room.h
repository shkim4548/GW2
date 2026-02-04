#pragma once
#include "Job.h"
#include "JobQueue.h"
#include "Protocol.pb.h"

namespace Navigation { class NavigationSystem; struct GridCell; class WalkableGrid; }

class Room : public JobQueue
{
public:
	Room();
	virtual ~Room();

	bool Enter(PlayerRef player);
	void Leave(int32 playerId);
	void Broadcast(SendBufferRef sendBuffer, int32 exceptId = -1);

	void SetRoomId(int32 roomId) { _roomId = roomId; }
	int32 GetRoomId() { return _roomId; }
	void SetRoomName(string roomName) { _roomName = roomName; }
	string GetRoomName() { return _roomName; }

	weak_ptr<Player> GetPlayerById(int32 id) { return weak_ptr<Player>(_players[id]); }

public:
	// Handlers
	bool HandleEnterPlayer(PlayerRef player);
	bool HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt);
	void HandleMovePlayer(Protocol::C_MOVE movePkt);

	// called by main thread
	void RunningRoom();

private:
	// internal
	void HandleMovePlayerInternal(PlayerRef player, vector<Navigation::GridCell*>& gridPath);
	void BroadcastMoving(const ObjectRef& obj);
	void BroadcastMovingEnd(const ObjectRef& obj);
	// 로비에서 호출해야함
	void UpdateRoom(float deltaTime);

private:
	unordered_map<int32, ObjectRef> _objects;
	unordered_map<int32, PlayerRef> _players;
	int32 _roomId;
	string _roomName;
	weak_ptr<Navigation::NavigationSystem> _navigationSystem;
	weak_ptr<Navigation::WalkableGrid> _roomWalkableGrid;
	bool _isRunning = false;
};

extern shared_ptr<Room> GRoom;

