#include "pch.h"
#include "CoreGlobal.h"
#include "ThreadManager.h"
#include "DeadLockProfiler.h"
#include "SendBuffer.h"
#include "GlobalQueue.h"
#include "JobTimer.h"

ThreadManager*      GThreadManager      = nullptr;
SendBufferManager*  GSendBufferManager  = nullptr;
GlobalQueue*        GGlobalQueue        = nullptr;
JobTimer*           GJobTimer           = nullptr;
DeadLockProfiler*   GDeadLockProfiler   = nullptr;

class CoreGlobal
{
public:
    CoreGlobal()
    {
        GThreadManager      = new ThreadManager();
        GSendBufferManager  = new SendBufferManager();
        GGlobalQueue        = new GlobalQueue();
        GJobTimer           = new JobTimer();
        GDeadLockProfiler   = new DeadLockProfiler();
    }

    ~CoreGlobal()
    {
        delete GThreadManager;
        delete GSendBufferManager;
        delete GGlobalQueue;
        delete GJobTimer;
        delete GDeadLockProfiler;
    }
} GCoreGlobal;