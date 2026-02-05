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
    GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Lobby] Load NavGrid start\n");

    bool ok = _navmeshLoader->LoadNavGridBin(
        "../../GW2_Client/Assets/NavMeshExport/navgrid.bin",
        *_walkableGrid
    );

    if (!ok) {
        GConsoleLogger->WriteStdOut(Color::RED, L"[Lobby] NavGrid load failed\n");
        return;
    }

    GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Lobby] NavGrid load complete\n");

    MakeRoom("TestRoom");
    GConsoleLogger->WriteStdErr(Color::YELLOW, L"[LobbyInit] Make Room roomCnt: ");
    cout << _rooms.size() << endl;

    // DEBUG
    cout << "[NavGrid Loaded]\n";
    cout << "width     : " << _walkableGrid->width << "\n";
    cout << "height    : " << _walkableGrid->height << "\n";
    cout << "cellSize  : " << _walkableGrid->cellSize << "\n";
    cout << "cellCount : " << _walkableGrid->cells.size() << endl;

    // 제거: BuildWalkableGrid는 파일 데이터를 덮어씀
    // _navigationSystem->BuildWalkableGrid(*_walkableGrid, ...);

    // 추가: Connections만 빌드
    _navigationSystem->BuildConnections(*_walkableGrid);

    // Init
    _navigationSystem->Init(*_walkableGrid);

    // 검증
    _navigationSystem->PrintGridSummary(*_walkableGrid);
    _navigationSystem->VerifyWorldGridInvariant(*_walkableGrid);

    // ===== 검증 로그 =====
    cout << "[Connections Verification - First 3x3]" << endl;
    for (int z = 0; z < min(3, _walkableGrid->height); ++z) {
        for (int x = 0; x < min(3, _walkableGrid->width); ++x) {
            auto& cell = _walkableGrid->At(x, z);
            if (cell.walkable) {
                cout << "Cell(" << x << "," << z << ") N="
                    << (int)cell.neighbors[0] << " E="
                    << (int)cell.neighbors[1] << " S="
                    << (int)cell.neighbors[2] << " W="
                    << (int)cell.neighbors[3] << endl;
            }
        }
    }
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

void Lobby::RunRooms()
{
    _isRunning = true;
    _lastUpdateTime = chrono::steady_clock::now();
    const auto TICK_INTERVAL = chrono::milliseconds(33);

    GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Lobby::RunRooms] Main loop Start : 30Hz\n");

    while (_isRunning)
    {
        auto frameStart = chrono::steady_clock::now();

        // deltaTime 계산
        auto now = chrono::steady_clock::now();
        float deltaTime = chrono::duration<float>(now - _lastUpdateTime).count();

        // Lobby Update
        LobbyUpdate(deltaTime);

        // Frame Rate 제한
        auto frameEnd = chrono::steady_clock::now();
        auto elapsed = chrono::duration_cast<chrono::milliseconds>(frameEnd - frameStart);
        if (elapsed < TICK_INTERVAL)
        {
            this_thread::sleep_for(TICK_INTERVAL - elapsed);
        }
        else
        {
            // 틱이 밀림
            if (elapsed.count() > 50)
            {
                GConsoleLogger->WriteStdErr(Color::YELLOW, L"[Lobby Warning] Tick Took : ");
                cout << elapsed.count();
                GConsoleLogger->WriteStdErr(Color::YELLOW, L"ms(target : 33ms)\n");
            }
        }
    }
    GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Lobby::RunRooms] Main loop Ended\n");

}

void Lobby::LobbyUpdate(float deltaTime)
{
    // TODO : MMR 레이팅에 따른 공개방 범위 구현
    // 모든 Room Update
    for(auto& [roomId, room] : _rooms)
    {
        if (room == nullptr)
        {
            GConsoleLogger->WriteStdErr(Color::RED, L"[Lobby Update] : Room id nullptr : ");
            cout << roomId << endl;
            continue;
        }
        room->UpdateRoom(deltaTime);
    }

    // Room 정리
    vector<int32> emptyRooms;
    for (auto& [roomId, room] : _rooms)
    {
        // TODO : 실행 상태를 확인해서 없애는 로직도 추가해야함
        if (room->GetRoomPlayerCount() == 0)
        {
            emptyRooms.push_back(roomId);
        }
    }

    for (int32 roomId : emptyRooms)
    {
        GConsoleLogger->WriteStdOut(Color::WHITE, L"[Lobby Update] Deleteing empty room : ");
        cout << roomId << endl;
        // 실제 룸 삭제
        //DeletedRoom();
    }
}