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
	void SetRoomName(wstring roomName) { _roomName = roomName; }
	wstring GetRoomName() { return _roomName; }

public:
	// Handlers
	bool HandleEnterPlayer(PlayerRef player);
	void HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt);

private:

private:
	unordered_map<int32, weak_ptr<Player>> _players;
	int32 _roomId;
	wstring _roomName;
};

extern shared_ptr<Room> GRoom;

