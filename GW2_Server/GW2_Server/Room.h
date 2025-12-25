#pragma once
#include "Job.h"
#include "JobQueue.h"
#include "Protocol.pb.h"

class Room : public JobQueue
{
public:
	Room();
	virtual ~Room();

	void Enter(PlayerRef player);
	void Leave(PlayerRef player);
	void Broadcast(SendBufferRef sendBuffer);

	void InitNavigation();

	void SetRoomId(int32 roomId) { _roomId = roomId; }
	int32 GetRoomId() { return _roomId; }
	void SetRoomName(string roomName) { _roomName = roomName; }
	string GetRoomName() { return _roomName; }

public:
	// Handlers
	bool HandleEnterPlayer(PlayerRef player);
	void HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt);

private:

private:
	unordered_map<int32, weak_ptr<Player>> _players;
	int32 _roomId;
	string _roomName;

	// ¸Ê Á¤º¸
	unique_ptr<class NavmeshLoader> _navmeshLoader;
	shared_ptr<struct OBJ_CollisionMesh> _collisionMesh;
};

extern shared_ptr<Room> GRoom;

