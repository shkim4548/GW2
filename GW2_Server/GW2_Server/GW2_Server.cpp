#include "pch.h"
#include "ThreadManager.h"
#include "Service.h"
#include "Session.h"
#include "GameSession.h"
#include "GameSessionManager.h"
#include "BufferWriter.h"
#include "ClientPacketHandler.h"
#include <tchar.h>
#include "Protocol.pb.h"
#include "Job.h"
#include "Lobby.h"
#include "Room.h"
#include "Player.h"
#include "DBConnectionPool.h"
#include "DBBind.h"
#include "XMLParser.h"
#include "DBSynchronizer.h"
#include "GenProcedures.h"

enum
{
	WORKER_TICK = 64
};

void DoWorkerJob(ServerServiceRef& service)
{
	while (true)
	{
		LEndTickCount = ::GetTickCount64() + WORKER_TICK;

		// 네트워크 코드 엔트리 포인트
		// 네트워크 입출력 처리 -> 인게임 로직 호출(패킷 핸들러)
		service->GetIocpCore()->Dispatch(10);

		// 예약된 작업들을 배분하는 부분을 만든다.
		ThreadManager::DistributeReservedJobs();

		// 글로벌 큐
		ThreadManager::DoGlobalQueueWork();
	}
}

int main()
{
	ASSERT_CRASH(GDBConnectionPool->Connect(1, L"Driver={ODBC Driver 17 for SQL Server};Server=(localdb)\\MSSQLLocalDB;Database=ServerDb;Trusted_Connection=Yes;"));

	DBConnection* dbConn = GDBConnectionPool->Pop();
	DBSynchronizer dbSync(*dbConn);
	dbSync.Synchronize(L"GameDB.xml");

	ClientPacketHandler::Init();
	// ServerService는 책임이 IOCP에 한정되어야한다.
	ServerServiceRef service = MakeShared<ServerService>(NetAddress(L"127.0.0.1", 7777), MakeShared<IocpCore>(), 	MakeShared<GameSession>, 100);

	ASSERT_CRASH(service->Start());

	// 로비 초기화
	GLobby->DoAsync(&Lobby::LobbyInit);


	// ServerService 생성
	for (int32 i = 0; i < 5; i++)
	{
		GThreadManager->Launch([&service]()
			{
				DoWorkerJob(service);
			});
	}

	// Main Thread -> main은 Contents를 위해 돌아가는 것으로 바꾼다.
	//DoWorkerJob(service);

	// 게임 Contents
	// TODO : while 조건 저렇게 두면 문제의 소지가 될 수 있다.
	uint64 lastTick = GetTickCount64();
	while (true)
	{
		//cout << "Lobby Update Loop in main is now running" << endl;
		uint64 now = GetTickCount64();
		float deltaTime = (now - lastTick) * 0.001f;
		// deltaTime 폭주 방지
		// Windows.h 매크로 지정 방지
		deltaTime = std::clamp(deltaTime, 0.0f, 0.1f);

		// 30 FPS
		if (deltaTime >= 0.033f)
		{
			lastTick = now;
			GLobby->LobbyUpdate(deltaTime);
		}

		// BusyLoop 방지
		Sleep(1);
	}

	GThreadManager->Join();
}