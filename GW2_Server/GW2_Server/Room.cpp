#include "pch.h"
#include "Room.h"
#include "Player.h"
#include "GameSession.h"

// 공용으로 사용할 전역 룸
shared_ptr<Room> GRoom = make_shared<Room>();	//모든 클라를 여기에 접속시켜서 확인한다.

Room::Room()
{
}

Room::~Room()
{
}

void Room::Enter(PlayerRef player)
{
}

void Room::Leave(PlayerRef player)
{
}

void Room::Broadcast(SendBufferRef sendBuffer)
{

}

void Room::InitNavigation()
{

}

bool Room::HandleEnterPlayer(PlayerRef player)
{
	return false;
}


void Room::HandleSkill(PlayerRef player, Protocol::C_SKILL skillPkt)
{
}
