#pragma once
class ObjectUtils
{
public:
	static PlayerRef CreatePlayer(GameSessionRef session);
	static MinionRef CreateMinion();
	static TurretRef CreateTurret();
	static NexusRef CreateNexus();
	static BaronRef CreateBaron();

private:
	static atomic<int32> s_idGenerator;
};

