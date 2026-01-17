#pragma once
#include <unordered_map>
#include "JobQueue.h"

/*----------
	Lobby
------------*/

class Room;
class Player;
namespace Navigation { class NavigationSystem; class WalkableGrid; }

class Lobby : public JobQueue
{
public:
	Lobby();
	virtual ~Lobby();

	void LobbyInit();

	void OnClientEnter(PlayerRef player);
	void OnClientLeave(int32 playerId);

	unordered_map<int32, RoomRef> GetRoomList();
	weak_ptr<Room> GetRoomById(int32 roomId);
	weak_ptr<Navigation::NavigationSystem> GetNavigationSystem() { return _navigationSystem; }

	RoomRef MakeRoom(string roomName);
	void DeleteRoom(int32 roomId);
	void EnterRoom(int32 roomId, int64 playerId);

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
};

extern shared_ptr<Lobby> GLobby;
