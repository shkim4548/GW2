#pragma once
#include "Job.h"
#include "JobQueue.h"
#include "Protocol.pb.h"
#pragma message("ROOM HEADER LOADED")

namespace Navigation { class NavigationSystem; struct GridCell; class WalkableGrid; struct LaneRoute; }
namespace GameMath { struct Vector3; }
class Minion;
class Turret;

class Room : public JobQueue
{
public:
	Room();
	virtual ~Room();

	bool Enter(PlayerRef gameObject);
	void Leave(int32 playerId);
	void Broadcast(SendBufferRef sendBuffer, int32 exceptId = -1);
	void RoomInit(unordered_map<int32, shared_ptr<Navigation::LaneRoute>> route);

	void SetRoomId(int32 roomId) { _roomId = roomId; }
	int32 GetRoomId() { return _roomId; }
	int32 GetRoomPlayerCount() { return _players.size(); }
	void SetRoomName(string roomName) { _roomName = roomName; }
	string GetRoomName() { return _roomName; }
	void SetIsRunning(bool isRunning) { _isRunning = isRunning; }

	unordered_map<int32, ObjectRef> GetRoomObjects() { return _objects; }

	weak_ptr<Player> GetPlayerById(int32 id) { return weak_ptr<Player>(_players[id]); }

public:
	// Handlers
	void HandleMovePlayer(Protocol::C_MOVE movePkt);
	bool HandleSpawnMinion(MinionRef minion);
	bool HandleStartGameFlag();

	// 로비에서 호출해야함
	void UpdateRoom(float deltaTime);
	shared_ptr<Minion> SpawnMinion(int32 laneId, Protocol::CampType team);
	shared_ptr<Turret> SpawnTurret(GameMath::Vector3 pos, Protocol::CampType team);
	shared_ptr<Nexus> SpawnNexus(GameMath::Vector3 pos, Protocol::CampType team);

	// Lane Controller
	void InitLaneRouteBin();
	void InitLaneRouteJson();

	// 골드 관련
	void GiveGold(PlayerRef player, int64 amount);
	void HandleBuyCard(PlayerRef player, int32 cardId);
	void HandleRemoveCard(PlayerRef player, int32 cardId);

public:
	// Object called
	void CollectEnemiesInRange(shared_ptr<Object> requester, float range);
	void HandleMinionMove(shared_ptr<Minion> minion, GameMath::Vector3 dest, float speed, float deltaTime, uint8 laneId, int32 wpIndex);
	void HandleMinionAttack(shared_ptr<Minion> attacker, shared_ptr<Object> target);
	void HandleChaseMove(shared_ptr<Minion> minion, GameMath::Vector3 dest, float speed, float deltaTime, uint8 laneId);
	void HandleRemoveObject(int32 targetId, int32 attackerId);
	void HandleTurretAttack(int32 attckerId, int32 targetId);
	void HandleNexusDead(Protocol::CampType deadTeam);

	// Players
	bool HandleEnterPlayer(PlayerRef player);
	bool HandleSkill(ObjectRef attacker, Protocol::C_SKILL skillPkt);
	void HandleAttack(int32 attackerId, int32 targetId, Protocol::SkillType commandId);
	void HandleRespawnPlayer(int32 playerId);

private:
	// internal
	void HandleMovePlayerInternal(PlayerRef player, std::vector<Navigation::GridCell*>& gridPath, const GameMath::Vector3& startWorld, const GameMath::Vector3& endWorld);
	void BroadcastMoving(const ObjectRef& obj);
	void BroadcastMovingEnd(const ObjectRef& obj);


	weak_ptr<Navigation::LaneRoute> GetLaneRoute(int32 laneId) const;
	void SetLaneRoute(int32 laneId, shared_ptr<Navigation::LaneRoute> route);
	// static
	static vector<GameMath::Vector3> SmoothPath(const vector<GameMath::Vector3>& path, const Navigation::WalkableGrid& grid, shared_ptr<Navigation::NavigationSystem> navSystem, uint8 laneId);
	void StartGame();
	void SyncObjectsToPlayer(PlayerRef newPlayer);

private:
	unordered_map<int32, ObjectRef> _objects;
	unordered_map<int32, PlayerRef> _players;
	int32 _roomId;
	string _roomName;

	// === Navigation System ===
	weak_ptr<Navigation::NavigationSystem> _navigationSystem;
	weak_ptr<Navigation::WalkableGrid> _roomWalkableGrid;
	bool _isRunning = false;
	
	// === laneId ===
	unordered_map<int32, shared_ptr<Navigation::LaneRoute>> _laneRoute;
	int32 _laneIdCnt = 1;

	// === minion cool time ===
	float _minionSpawnCoolDown = 5.0f;
	float _minionSpawnAccumulate = 0.0f;

	// === minion spawn ===
	static constexpr int32 WAVE_MINION_COUNT = 5;    // 레인당 마리 수
	static constexpr float WAVE_SPAWN_INTERVAL = 5.0f; // 쌍 사이 간격
	static constexpr int32 MINION_LANE_TOP = 1;
	static constexpr int32 MINION_LANE_BOT = 3;
	static constexpr int32 MAX_MINION_COUNT = 50;
	deque<int32> _minionSpawnOrder;                 // ← 추가 (FIFO 삭제용)

	bool  _isSpawningWave = false;
	int32 _waveSpawnCount = 0;
	float _waveSpawnAccumulate = 0.0f;

	unordered_map<int32, float> _respawnTimers; // playerId → 남은 시간(초)
	
	// === Gold ===
	float _goldIncomeTimer = 0.0f;
	static constexpr float GOLD_INCOME_INTERVAL = 1.0f;  // 1초마다
	static constexpr int64 GOLD_INCOME_AMOUNT = 20;

	// Game Loop
	bool _gameStarted = false;
};

//extern shared_ptr<Room> GRoom;