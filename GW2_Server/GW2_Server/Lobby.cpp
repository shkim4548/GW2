#include "pch.h"
#include "Lobby.h"

#include "ObjectUtils.h"
#include "Player.h"
#include "Room.h"
#include "NavmeshLoader.h"
#include "NavigationSystem.h"

LobbyRef GLobby = make_shared<Lobby>();	//모든 클라를 여기에 접속시켜서 확인한다.

Lobby::Lobby()
{
	//LobbyInit();
	_navigationSystem = MakeShared<Navigation::NavigationSystem>();
	_walkableGrid = MakeShared<Navigation::WalkableGrid>();
	//cout << "Lobby Construct" << endl;
}

Lobby::~Lobby()
{
	_rooms.clear();
	_lobbyPlayers.clear();
	cout << "Lobby Destroy" << endl;
}

void Lobby::LobbyInit()
{
	GConsoleLogger->WriteStdOut(
		Color::YELLOW,
		L"[Lobby] Load NavGrid start\n"
	);

	bool ok = _navmeshLoader->LoadNavGridBin(
		"../../GW2_Client/Assets/NavMeshExport/navgrid.bin",
		*_walkableGrid
	);

	if (!ok)
	{
		GConsoleLogger->WriteStdOut(
			Color::RED,
			L"[Lobby] NavGrid load failed\n"
		);
		return;
	}

	GConsoleLogger->WriteStdOut(
		Color::YELLOW,
		L"[Lobby] NavGrid load complete\n"
	);
	//Navigation::WalkableGrid walkGrid = *_walkableGrid;
	// TODO : HardCoding
	MakeRoom("TestRoom");
	GConsoleLogger->WriteStdErr(Color::YELLOW, L"[LobbyInit] Make Room roomCnt: ");
	cout << _rooms.size() << endl;

	// DEBUG
	//Navigation::NavigationSystem& navSystem = *_navigationSystem;
	cout << "[NavGrid Loaded]\n";
	cout << "width     : " << _walkableGrid->width << "\n";
	cout << "height    : " << _walkableGrid->height << "\n";
	cout << "cellSize  : " << _walkableGrid->cellSize << "\n";
	cout << "cellCount : " << _walkableGrid->cells.size() << endl;
	_navigationSystem->PrintGridSummary(*_walkableGrid);
	//navSystem.PrintGrid(*_walkableGrid);
	_navigationSystem->VerifyWorldGridInvariant(*_walkableGrid);
	_navigationSystem->BuildWalkableGrid(*_walkableGrid, _walkableGrid->width, _walkableGrid->height, _walkableGrid->cellSize, _walkableGrid->origin);
	_navigationSystem->Init(*_walkableGrid);
}

void Lobby::OnClientEnter(PlayerRef player)
{
	if (player == nullptr)
		return;

	int64 id = player->GetPlayerId();
	cout << "[OnClientEnter] GetPlayerId : " << id << '\n';
	_lobbyPlayers[id] = player;
	cout << "lobbyPlayer size : " << _lobbyPlayers.size() << '\n';

	// TODO : RoomId 선택 혹은 랜덤 수를 넣을 수 있도록 해줘야한다.
	//EnterRoom(0, id);
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

RoomRef Lobby::MakeRoom(string roomName)
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
		// 에러 발생 부분
		GConsoleLogger->WriteStdErr(Color::RED, L"[EnterRoom] player is nullptr\n");
		return;
	}
	_lobbyPlayers.erase(playerId);
	_rooms[roomId]->Enter(player);
}

void Lobby::LobbyUpdate(float deltaTime)
{
	for (auto& [roomId, room] : _rooms)
	{
		room->DoAsync(&Room::UpdateRoom, deltaTime);
	}
}