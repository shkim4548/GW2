#include "pch.h"
#include "Minion.h"
#include "Room.h"

Minion::Minion()
{
}

void Minion::UpdateMinion(float deltaTime)
{
	switch (_minionState)
	{
	case Protocol::MinionState::MINION_IDLE:
		UpdateIdle(deltaTime);
		break;
	case Protocol::MinionState::MINION_LINE_TRACE:
		UpdateLaneTrace(deltaTime);
		break;
	case Protocol::MinionState::MINION_CHASE_TARGET:
		UpdateLaneTrace(deltaTime);
		break;
	case Protocol::MinionState::MINION_ATTACK:
		UpdateChaseTarget(deltaTime);
		break;
	case Protocol::MinionState::MINION_DEAD:
		// TODO : 미니언 제거 후 보상 지급
		break;
	}
}

void Minion::UpdateIdle(float deltaTime)
{

}

void Minion::UpdateLaneTrace(float deltaTime)
{
	
}

void Minion::UpdateChaseTarget(float deltaTime)
{
}

void Minion::UpdateAttack(float deltaTime)
{
}

weak_ptr<Object> Minion::FindBestTarget()
{
	vector<shared_ptr<Object>> targets;
	_room->CollectEnemiesInRange(shared_ptr<Minion>(this), _detectionRange, OUT targets);

	//weak_ptr<Object> bestTarget = nullptr;
}

int32 Minion::GetTargetPriority(Object* obj)
{
	// TODO : 아군 공격 금지
	if (obj->GetTeamFlag() == GetTeamFlag())
		return -1;

	Protocol::ObjectType type = obj->GetObjectType();
	if (type == Protocol::ObjectType::OBJECT_TYPE_MINION)
	{
		return 1;
	}
	else if (type == Protocol::ObjectType::OBJECT_TYPE_TURRET)
	{
		return 2;
	}
	else if (type == Protocol::ObjectType::OBJECT_TYPE_PLAYER)
	{
		return 3;
	}
	else if (type == Protocol::ObjectType::OBJECT_TYPE_NEXUS)
	{
		return 4;
	}
	else
	{
		GConsoleLogger->WriteStdErr(Color::RED, L"[Minion] GetTargetPriority, Target Type is invalid\n");
		return 0;
	}
}
