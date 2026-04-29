#include "pch.h"
#include "ThreadManager.h"
#include "Service.h"
#include "Session.h"
#include "GameSession.h"
#include "GameSessionManager.h"
#include "BufferWriter.h"
#include "ClientPacketHandler.h"
#include "Protocol.pb.h"
#include "Job.h"
#include "Lobby.h"
#include "Room.h"
#include "Player.h"

#ifdef _WIN32
#include <tchar.h>
#include "DBConnectionPool.h"
#include "DBBind.h"
#include "XMLParser.h"
#include "DBSynchronizer.h"
#include "GenProcedures.h"
#else
#include "IoCore.h"
#include "IoUtils.h"
#endif

// 플랫폼별 Tick 통일
#ifdef _WIN32
#define GET_TICK() ::GetTickCount64()
#else
#define GET_TICK() GetCurrentTick()
#endif

enum
{
    WORKER_TICK = 64
};

void DoWorkerJob(ServerServiceRef& service)
{
    while (true)
    {
        LEndTickCount = GET_TICK() + WORKER_TICK;

        // 네트워크 입출력 처리
#ifdef _WIN32
        service->GetIocpCore()->Dispatch(10);
#else
        service->GetIoCore()->RunFor(10);
#endif

        ThreadManager::DistributeReservedJobs();
        ThreadManager::DoGlobalQueueWork();
    }
}

int main()
{
#ifdef _WIN32
    ASSERT_CRASH(GDBConnectionPool->Connect(1, L"Driver={ODBC Driver 17 for SQL Server};Server=(localdb)\\MSSQLLocalDB;Database=ServerDb;Trusted_Connection=Yes;"));
    DBConnection* dbConn = GDBConnectionPool->Pop();
    DBSynchronizer dbSync(*dbConn);
    dbSync.Synchronize(L"GameDB.xml");
#endif

    ClientPacketHandler::Init();

#ifdef _WIN32
    ServerServiceRef service = MakeShared<ServerService>(
        NetAddress(L"127.0.0.1", 7777),
        MakeShared<IocpCore>(),
        MakeShared<GameSession>,
        100);
#else
    IoCoreRef ioCore = make_shared<IoCore>();
    GIoCore = ioCore.get();

    ServerServiceRef service = make_shared<ServerService>(
        NetAddress("127.0.0.1", 7777),
        ioCore,
        []() { return make_shared<GameSession>(); },
        100);
#endif

    ASSERT_CRASH(service->Start());

    // 로비 초기화
    GLobby->DoAsync(&Lobby::LobbyInit);

    // Worker 스레드 생성
    for (int32 i = 0; i < 5; i++)
    {
        GThreadManager->Launch([&service]()
            {
                DoWorkerJob(service);
            });
    }

    // 게임 Contents (Main Thread)
    uint64 lastTick = GET_TICK();
    while (true)
    {
        uint64 now = GET_TICK();
        float deltaTime = (now - lastTick) * 0.001f;
        deltaTime = std::clamp(deltaTime, 0.0f, 0.1f);

        if (deltaTime >= 0.033f)
        {
            lastTick = now;
            GLobby->LobbyUpdate(deltaTime);
        }

#ifdef _WIN32
        Sleep(1);
#else
        this_thread::sleep_for(chrono::milliseconds(1));
#endif
    }

    GThreadManager->Join();
}