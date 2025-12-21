#pragma once

#define WIN32_LEAN_AND_MEAN // ���� ������ �ʴ� ������ Windows ������� �����մϴ�.

#ifdef _DEBUG
#pragma comment(lib, "TR_ServerCore\\Debug\\TR_ServerCore.lib")
#pragma comment(lib, "Protobuf\\Debug\\libprotobufd.lib")
#else
#pragma comment(lib, "ServerCore\\Release\\TR_ServerCore.lib")
#pragma comment(lib, "Protobuf\\Release\\libprotobuf.lib")
#endif

#include "CorePch.h"
#include "Enum.pb.h"
#include "Struct.pb.h"
#include <optional>

using GameSessionRef = shared_ptr<class GameSession>;
using PlayerRef = shared_ptr<class Player>;
using RoomRef = shared_ptr<class Room>;
using LobbyRef = shared_ptr<class Lobby>;

//USING_SHARED_PTR(GameSession);
//USING_SHARED_PTR(Player);
//USING_SHARED_PTR(Monster);
//USING_SHARED_PTR(Craeture);
//USING_SHARED_PTR(Object);
//USING_SHARED_PTR(Room);
//USING_SHARED_PTR(Map);
//USING_SHARED_PTR(Bullet);

#define SEND_PACKET(pkt)	\
	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(pkt);	\
	session->Send(sendBuffer);