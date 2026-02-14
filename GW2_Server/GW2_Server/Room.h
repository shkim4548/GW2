#pragma once
#include "Job.h"
#include "JobQueue.h"
#include "Protocol.pb.h"
#pragma message("ROOM HEADER LOADED")

namespace Navigation { class NavigationSystem; struct GridCell; class WalkableGrid; }
namespace GameMath { struct Vector3; }
class Minion;

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
	int32 GetRoomPlayerCount() { return _players.size(); }
	void SetRoomName(string roomName) { _roomName = roomName; }
	string GetRoomName() { return _roomName; }

	weak_ptr<Player> GetPlayerById(int32 id) { return weak_ptr<Player>(_players[id]); }

public:
	// Handlers
	bool HandleEnterPlayer(PlayerRef player);
	bool HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt);
	void HandleMovePlayer(Protocol::C_MOVE movePkt);

	// 로비에서 호출해야함
	void UpdateRoom(float deltaTime);

public:
	// Object called
	void CollectEnemiesInRange(const shared_ptr<Object> requester, float range, vector<shared_ptr<Object>>& OUT targets) const;
	void HandleMinionMove(shared_ptr<Minion> minion, const GameMath::Vector3& dest, float speed, float deltaTime, uint8 laneId);
	void HandleMinionAttack(shared_ptr<Object> target);

private:
	// internal
	void HandleMovePlayerInternal(PlayerRef player, std::vector<Navigation::GridCell*>& gridPath, const GameMath::Vector3& startWorld, const GameMath::Vector3& endWorld);
	void BroadcastMoving(const ObjectRef& obj);
	void BroadcastMovingEnd(const ObjectRef& obj);

	void DeleteRoom();

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

