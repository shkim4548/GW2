#include "pch.h"
#include "Session.h"
#include "IoCore.h"
#include "CoreGlobal.h"
#include "Service.h"

Session::Session() : _socket(GIoCore->GetContext())
{

}

Session::~Session()
{

}

void Session::Send(SendBufferRef sendBuffer)
{
	if (_connected)
	{
		return;
	}


}