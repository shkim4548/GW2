#pragma once
#include "Object.h"

class Nexus : public Object
{
public:
	Nexus();
	virtual ~Nexus();

	void InitNexus(shared_ptr<Room> room, Protocol::CampType team);
	void SetNexusId(int32 id) { _objectId = id; }

protected:
	virtual void OnDead() override;
	virtual void UpdateController(float deltaTime) override { }
};

