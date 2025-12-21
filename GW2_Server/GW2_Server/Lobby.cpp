#include "pch.h"
#include "Lobby.h"

#include "pch.h"
#include "ObjectUtils.h"
#include "Lobby.h"
//#include "Object.h"
#include "Player.h"
#include "Room.h"
#include <unordered_map>

LobbyRef GLobby = make_shared<Lobby>();	//모든 클라를 여기에 접속시켜서 확인한다.

Lobby::Lobby()
{
	//LobbyInit();
}

Lobby::~Lobby()
{
	_rooms.clear();
	_lobbyPlayers.clear();
}

void Lobby::LobbyInit()
{
	// 로비를 만들어서 줍시다
	if (_rooms.empty())
	{
		RoomRef room = MakeRoom(L"DefaultRoom");
	}
	GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Lobby] LobbyInit : Create Room\n");
}

void Lobby::OnClientEnter(PlayerRef player)
{
	if (player == nullptr)
		return;

	int64 id = player->GetPlayerId();
	cout << "[OnClientEnter] GetPlayerId : " << id << '\n';
	_lobbyPlayers[id] = player;
	cout << "lobbyPlayer size : " << _lobbyPlayers.size() << '\n';
}

void Lobby::OnClientLeave(int32 playerId)
{
	cout << "OnClientLeave" << endl;
	_lobbyPlayers.erase(playerId);
}

unordered_map<int32, RoomRef> Lobby::GetRoomList()
{
	return _rooms;
}

weak_ptr<Room> Lobby::GetRoomById(int32 roomId)
{
	return _rooms[roomId];
}

RoomRef Lobby::MakeRoom(wstring roomName)
{
	// 방을 하나 만들어서 로비의 목록에 저장한다.
	RoomRef newRoom = MakeShared<Room>();
	newRoom->SetRoomName(roomName);
	newRoom->SetRoomId(_id);
	_rooms.emplace(_id, newRoom);
	return newRoom;
}

void Lobby::DeleteRoom(int32 roomId)
{
	_rooms.erase(roomId);
}

void Lobby::EnterRoom(int32 roomId, int64 playerId)
{
	// Lobby에서 빼주고, Room에 플레이어를 넣어주자.
	PlayerRef player = _lobbyPlayers[playerId];
	if (player == nullptr)
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[EnterRoom] player is nullptr\n");
		return;
	}
	_lobbyPlayers.erase(playerId);
	_rooms[roomId]->Enter(player);
}
