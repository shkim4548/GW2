#pragma once
#include <vector>
#include "GameMath.h"

struct OBJ_CollisionMesh
{
	vector<GameMath::Vector3> Vertices;
	vector<Triangle> Triangles;
};

class NavmeshLoader
{
public:
	void LoadObjFile(const char* filePath, OBJ_CollisionMesh& mesh);
};

