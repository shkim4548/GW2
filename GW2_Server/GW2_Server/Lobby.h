#pragma once
#include <unordered_map>
#include "JobQueue.h"
#include "StatLoader.h"

/*----------
	Lobby
------------*/

class Room;
class Player;
struct UnitStat;
namespace Navigation { class NavigationSystem; class WalkableGrid; struct LaneRoute; }

class Lobby : public JobQueue
{
public:
	Lobby();
	virtual ~Lobby();

	void LobbyInit();

	void OnClientEnter(PlayerRef player);
	void OnClientLeave(int32 playerId);

	// Getters
	unordered_map<int32, RoomRef> GetRoomList();
	weak_ptr<Room> GetRoomById(int32 roomId);
	weak_ptr<Navigation::NavigationSystem> GetNavigationSystem() { return _navigationSystem; }
	weak_ptr<Navigation::WalkableGrid> GetWalkableGrid() { return _walkableGrid; }
	unordered_map<int32, PlayerRef> GetLobbyPlayers() { return _lobbyPlayers; }

	// Json Stats
	UnitStat GetUnitStat(const string& type);
	CardStat GetCardStat(int32 cardId);
	const unordered_map<int32, CardStat>& GetCardStats() const { return _cardStats; }

	RoomRef FindOrCreateRoom(int32 gameMode, int32 maxPlayers);
	RoomRef MakeRoom(string roomName);
	void DeleteRoom(int32 roomId);
	void EnterRoom(int32 roomId, int64 playerId);

	/*--------------------
		called by main
	----------------------*/
	void RunRooms();
	void LobbyUpdate(float deltaTime);

private:

protected:
	unique_ptr<class LaneRouteLoader> _navRouteLoader;
	unordered_map<int32, shared_ptr<Navigation::LaneRoute>> _route;

private:
	// object 단위에서 자신의 소속 room을 갖고 있다.
	unordered_map<int32, RoomRef> _rooms;
	unordered_map<int32, PlayerRef> _lobbyPlayers;
	int32 _id = 0;

	// 맵 정보
	unique_ptr<class NavmeshLoader> _navmeshLoader;
	shared_ptr<struct OBJ_CollisionMesh> _collisionMesh;
	shared_ptr<Navigation::NavigationSystem> _navigationSystem;
	shared_ptr<Navigation::WalkableGrid> _walkableGrid;

	// 타이밍
	bool _isRunning = false;
	chrono::steady_clock::time_point _lastUpdateTime;

	// stat 초기 데이터
	unordered_map<string, UnitStat> _unitStats;
	unordered_map<int32, CardStat> _cardStats;
	unique_ptr<class StatLoader> _statLoader;
};

extern shared_ptr<Lobby> GLobby;
