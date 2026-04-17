#include "pch.h"
#include "IoCore.h"

IoCore::IoCore()
	:_workGuard(asio::make_work_guard(_ioContext))
{

}

IoCore::~IoCore()
{
	Stop();
}

void IoCore::Run()
{
	_ioContext.run();
}

void IoCore::Stop()
{
	_workGuard.reset();
	_ioContext.stop();
}