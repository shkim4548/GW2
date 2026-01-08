#pragma once
#include "GameLogic.h"
#include "NavigationSystem.h"

namespace Navigation { class WalkableGrid; }

struct OBJ_CollisionMesh
{
	vector<GameMath::Vector3> Vertices;
	vector<Triangle> Triangles;   // 반드시 필요
};

struct NavmeshBinaryData
{
	vector<Triangle> triangles;
	Navigation::WalkableGrid grid;
};

class NavmeshLoader
{
public:
	void LoadObjFile(const char* filePath, OBJ_CollisionMesh& mesh);
	bool LoadMeshBin(vector<Triangle>& outTriangles, Navigation::WalkableGrid& outGrid, const string& path);
	bool LoadJsonMeshFile(const string& path, vector<GameMath::Vector3>& outVertices, vector<int32>& outIndices);
	bool LoadNavGridBin(const string& path, Navigation::WalkableGrid& OUT outGrid);
};