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

public:
	// Handlers
	bool HandleEnterPlayer(PlayerRef player);
	bool HandleMove(Protocol::C_MOVE pkt);
	void HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt);

private:


private:
	map<uint64, PlayerRef> _players;
	//JobQueue _jobs;
};

extern shared_ptr<Room> GRoom;

