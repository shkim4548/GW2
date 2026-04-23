#include "pch.h"
#include "CoreGlobal.h"
#include "ThreadManager.h"
#include "DeadLockProfiler.h"
#include "SendBuffer.h"
#include "GlobalQueue.h"
#include "IoCore.h"
#include "JobTimer.h"

ThreadManager*      GThreadManager      = nullptr;
SendBufferManager*  GSendBufferManager  = nullptr;
GlobalQueue*        GGlobalQueue        = nullptr;
JobTimer*           GJobTimer           = nullptr;
DeadLockProfiler*   GDeadLockProfiler   = nullptr;

IoCore*             GIoCore = nullptr;

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
        GIoCore             = new IoCore();
    }

    ~CoreGlobal()
    {
        delete GThreadManager;
        delete GSendBufferManager;
        delete GGlobalQueue;
        delete GJobTimer;
        delete GDeadLockProfiler;
        delete GIoCore;
    }
} GCoreGlobal;