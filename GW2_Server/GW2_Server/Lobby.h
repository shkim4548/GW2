#pragma once
#include <unordered_map>
#include "JobQueue.h"

/*----------
	Lobby
------------*/

class Room;
class Player;

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

	RoomRef MakeRoom(wstring roomName);
	void DeleteRoom(int32 roomId);
	void EnterRoom(int32 roomId, int64 playerId);

private:
	// object 단위에서 자신의 소속 room을 갖고 있다.
	unordered_map<int32, RoomRef> _rooms;
	unordered_map<int32, PlayerRef> _lobbyPlayers;
	int32 _id = 0;
};

extern shared_ptr<Lobby> GLobby;
