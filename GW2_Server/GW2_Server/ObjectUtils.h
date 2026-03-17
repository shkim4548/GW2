#pragma once
class ObjectUtils
{
public:
	static PlayerRef CreatePlayer(GameSessionRef session);
	static MinionRef CreateMinion();
	static TurretRef CreateTurret();

private:
	static atomic<int32> s_idGenerator;
};

