#pragma once
#include "NetAddress.h"
#include "RecvBuffer.h"

class Service;

/*-------------
	Session
---------------*/

class Session : public enable_shared_from_this<Session>
{
	friend class Listener;
	friend class Service;

	enum { BUFFER_SIZE = 0x10000 };

public:
	Session(asio::io_context& ioc);
	virtual ~Session();

public:
	// 외부에서 사용한다.
	void Send(SendBufferRef sendBuffer);
	bool Connect();
	void Disconnect(const string& cause);

	shared_ptr<Service> GetService() { return _service.lock(); }
	void  SetService(shared_ptr<Service> service) { _service = service; }

public:
	// 정보 접근
	void SetNetAddress(NetAddress address) { _netAddress = address; }
	bool IsConnected() { return _connected; };
	SessionRef GetSessionRef() { return static_pointer_cast<Session>(shared_from_this()); }
	asio::ip::tcp::socket& GetSocket() { return _socket; }

private:
	// 내부 등록
	bool                RegisterConnect();
	void                RegisterRecv();
	void                RegisterSend();

	void                ProcessConnect();
	void                ProcessDisconnect();
	void                ProcessRecv(int32 numOfBytes);
	void                ProcessSend(int32 numOfBytes);

	void                HandleError(const asio::error_code& ec);

private:
	weak_ptr<Service> _service;
	asio::ip::tcp::socket _socket;
	NetAddress _netAddress = {};
	Atomic<bool> _connected = false;

	// 수신 버퍼
	USE_LOCK;
	RecvBuffer _recvBuffer;
	//손신 버퍼
	Queue<SendBufferRef>        _sendQueue;
	atomic<bool>                _sendRegistered = false;
	vector<SendBufferRef>       _sendingBuffers; // SendEvent::sendBuffers 대체
};

/*-----------------
	PacketSession
------------------*/
struct PacketHeader
{
	uint16 size;
	uint16 id;
};

class PacketSession : public Session
{
public:
	PacketSession(asio::io_context& ioc);
};