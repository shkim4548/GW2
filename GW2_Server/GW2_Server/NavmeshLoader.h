#pragma once
#include "GameLogic.h"

struct OBJ_CollisionMesh
{
	vector<Vector3> Vertices;
	vector<Triangle> Triangles;
};

class NavmeshLoader
{
	void LoadObjFile(const char* filePath, OBJ_CollisionMesh& mesh);
};

