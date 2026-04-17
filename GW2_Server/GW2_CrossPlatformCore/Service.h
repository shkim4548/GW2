#pragma once
#include "NetAddress.h"
#include "IoCore.h"

enum class ServiceType : uint8
{
	Server,
	Client
};

using SessionFactory = function<SessionRef(void)>;

/*-------------
	Service
--------------*/

class Service
{

	
};

