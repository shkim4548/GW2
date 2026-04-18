#include "pch.h"
#include "Service.h"

/*------------
	Service
--------------*/

Service::Service(ServiceType type, NetAddress address, IoCoreRef core, SessionFactory factory, int32 maxSessionCount)
	: _type(type), _netAddress(address), _ioCore(core), _sessionFactory(factory), _maxSessionCount(maxSessionCount)
{

}

Service::~Service()
{

}

void Service::CloseService()
{

}

void Service::Broadcast(SendBufferRef sendBuffer)
{
	WRITE_LOCK;
	for (const auto& session : _sessions)
	{
		session->Send(sendBuffer);
	}
}

SessionRef Service::CreateSession()
{
	SessionRef session = _sessionFactory();
	session->SetService(shared_from_this());
	return session;
}

void Service::AddSession(SessionRef session)
{
	WRITE_LOCK;
	_sessionCount++;
	_sessions.insert(session);
}

void Service::ReleaseSession(SessionRef session)
{
	
}