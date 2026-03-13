#pragma once
#include "Object.h"

class Turret : Object
{
protected:
	virtual void OnDead() override;
};

