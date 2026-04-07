#include "pch.h"
#include "Lobby.h"

#include "ObjectUtils.h"
#include "Player.h"
#include "Room.h"
#include "NavmeshLoader.h"
#include "LaneRouteLoader.h"
#include "NavigationSystem.h"
#include "StatLoader.h"

LobbyRef GLobby = make_shared<Lobby>();	//모든 클라를 여기에 접속시켜서 확인한다.

Lobby::Lobby()
{
	//LobbyInit();
	_navigationSystem = MakeShared<Navigation::NavigationSystem>();
	_walkableGrid = MakeShared<Navigation::WalkableGrid>();
    _navmeshLoader = make_unique<NavmeshLoader>();
    _navRouteLoader = make_unique<LaneRouteLoader>();
    _statLoader = make_unique<StatLoader>();
	//cout << "Lobby Construct" << endl;
}

Lobby::~Lobby()
{
	_rooms.clear();
	_lobbyPlayers.clear();
    GConsoleLogger->WriteStdOut(Color::YELLOW, L"Lobby Destroctor has been called\n");
}

void Lobby::LobbyInit()
{
    GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Lobby] Load NavGrid start\n");

    bool ok = _navmeshLoader->LoadNavGridBin(
        "../../GW2_Client/Assets/NavMeshExport/navgrid.bin",
        *_walkableGrid
    );
    GConsoleLogger->WriteStdOut(Color::YELLOW,
        L"[NavGrid Spec] origin=(%.3f, %.3f) width=%d height=%d cellSize=%.3f\n",
        _walkableGrid->origin._x,
        _walkableGrid->origin._z,
        _walkableGrid->width,
        _walkableGrid->height,
        _walkableGrid->cellSize);
    if (!ok)
    {
        GConsoleLogger->WriteStdOut(Color::RED, L"[Lobby] NavGrid load failed\n");
        return;
    }

    GConsoleLogger->WriteStdOut(Color::YELLOW, L"[Lobby] NavGrid load complete\n");

    _navigationSystem->BuildConnections(*_walkableGrid);

    bool okLaneMap = _navmeshLoader->LoadLaneMap(
        "../../GW2_Client/Assets/NavMeshExport/laneMap.bin",
        *_walkableGrid);
    if (!okLaneMap)
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[Lobby] LaneMap load failed\n");
        return;
    }

    _navigationSystem->Init(*_walkableGrid);

    _navigationSystem->PrintGridSummary(*_walkableGrid);
    _navigationSystem->VerifyWorldGridInvariant(*_walkableGrid);

    // === 기존처럼 3x3 neighbor + laneId 간략 덤프 추가해도 좋음 (디버그용) ===
    cout << "[NavGrid Loaded]\n";
    cout << "width     : " << _walkableGrid->width << "\n";
    cout << "height    : " << _walkableGrid->height << "\n";
    cout << "cellSize  : " << _walkableGrid->cellSize << "\n";
    cout << "cellCount : " << _walkableGrid->cells.size() << endl;

    cout << "[Connections Verification - First 3x3]" << endl;
    for (int z = 0; z < min(3, _walkableGrid->height); ++z)
    {
        for (int x = 0; x < min(3, _walkableGrid->width); ++x)
        {
            auto& cell = _walkableGrid->At(x, z);
            if (cell.walkable)
            {
                cout << "Cell(" << x << "," << z << ") laneId="
                    << (int)cell.laneId
                    << " N=" << (int)cell.neighbors[0]
                    << " E=" << (int)cell.neighbors[1]
                    << " S=" << (int)cell.neighbors[2]
                    << " W=" << (int)cell.neighbors[3] << endl;
            }
        }
    }

    // LaneRoute(waypoints)는 여전히 미니언 중앙선용으로 Room 에게만 넘김
    bool loadMinionLane = _navRouteLoader->LoadLaneRoutesFromJson(
        "../../GW2_Client/Assets/NavMeshExport/laneRoutes.json",
        _route);
    if (!loadMinionLane)
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[Lobby] Minion LaneRoute load failed\n");
        return;
    }
    _navigationSystem->DebugCheckLaneRouteCoverage(*_walkableGrid, _route, *_navigationSystem);

    bool okStats = _statLoader->LoadUnitStatsFromJson("../Data/Stats.json", _unitStats);
    if (!okStats)
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[Lobby] UnitStats load failed\n");
        return;
    }
    StatLoader statLoader;
    statLoader.LoadCardStatsFromJson("../Data/Stats.json", _cardStats);

    shared_ptr<Room> room = MakeRoom("TestRoom");
    room->DoAsync(&Room::RoomInit, _route);

    GConsoleLogger->WriteStdErr(Color::YELLOW, L"[LobbyInit] Make Room roomCnt: ");
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
    auto it = _rooms.find(roomId);
    if (it == _rooms.end())
        return weak_ptr<Room>();
    return it->second;
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

    auto it = _rooms.find(roomId);      // ← 추가
    if (it == _rooms.end())             // ← 추가
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"[EnterRoom] room %d not found\n", roomId);
        return;
    }

    _lobbyPlayers.erase(playerId);
    it->second->Enter(player);          // ← _rooms[roomId] 대신
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
        //GConsoleLogger->WriteStdOut(Color::WHITE, L"[Lobby Update] Deleteing empty room : ");
        //cout << roomId << endl;
        // 실제 룸 삭제
        //DeletedRoom();
    }
}

UnitStat Lobby::GetUnitStat(const string& type)
{
    auto it = _unitStats.find(type);
    if (it != _unitStats.end()) return it->second;
    GConsoleLogger->WriteStdErr(Color::RED, L"[Lobby] UnitStat not found\n");
    return UnitStat{};
}

CardStat Lobby::GetCardStat(int32 cardId)
{
    auto it = _cardStats.find(cardId);
    if (it != _cardStats.end()) 
        return it->second;
    GConsoleLogger->WriteStdErr(Color::RED, L"[Lobby] CardStat not found id=%d\n", cardId);
    return CardStat{};
}
