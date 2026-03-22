#include "pch.h"
#include "Nexus.h"
#include "StatLoader.h"
#include "Lobby.h"
#include "Room.h"

Nexus::Nexus()
{
	_objectType = Protocol::OBJECT_TYPE_NEXUS;
}

Nexus::~Nexus()
{
}

void Nexus::InitNexus(shared_ptr<Room> room, Protocol::CampType team)
{
	_room = room;
	_campType = team;

	UnitStat stat = GLobby->GetUnitStat("Nexus");
	if (stat.hp > 0)
	{
		_statInfo.set_hp(stat.hp);
		_statInfo.set_max_hp(stat.maxHp);
	}
	else
	{
		_statInfo.set_hp(5000);
		_statInfo.set_max_hp(5000);
	}
}

void Nexus::OnDead()
{
	shared_ptr<Room> room = _room.lock();
	if (room == nullptr)
	{
		return;
	}
	room->DoAsync(&Room::HandleNexusDead, _campType);
}
