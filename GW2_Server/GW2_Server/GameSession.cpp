#include "pch.h"
#include "GameSession.h"
#include "GameSessionManager.h"
#include "ClientPacketHandler.h"
#include "Room.h"

void GameSession::OnConnected()
{
	GSessionManager.Add(static_pointer_cast<GameSession>(shared_from_this()));
	cout << "OnConnected" << '\n';
}

void GameSession::OnDisconnected()
{
	GSessionManager.Remove(static_pointer_cast<GameSession>(shared_from_this()));
	if (_currentPlayer)
	{
		if (auto room = _room.lock())	//weak 포인터를 shared 포인터로 lock을 통해 변환
			room->DoAsync(&Room::Leave, _currentPlayer);
	}
	_currentPlayer = nullptr;	// 기존의 ref카운트를 날려버린다.
	_players.clear();
}

void GameSession::OnRecvPacket(BYTE* buffer, int32 len)
{
	PacketSessionRef session = GetPacketSessionRef();
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	cout << "OnRecv" << '\n';
	// TODO : packetId 대역 체크
	ClientPacketHandler::HandlePacket(session, buffer, len);
}

void GameSession::OnSend(int32 len)
{
}