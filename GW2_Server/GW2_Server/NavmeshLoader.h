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

struct LaneMapHeader
{
	uint32 magic;		// LNMP
	uint16 version;		// 1
	uint16 reserved;	// 0;
	int32 width;
	int32 height;
};

class NavmeshLoader
{
public:
	void LoadObjFile(const char* filePath, OBJ_CollisionMesh& mesh);
	bool LoadMeshBin(vector<Triangle>& outTriangles, Navigation::WalkableGrid& outGrid, const string& path);
	bool LoadJsonMeshFile(const string& path, vector<GameMath::Vector3>& outVertices, vector<int32>& outIndices);
	bool LoadNavGridBin(const string& path, Navigation::WalkableGrid& OUT outGrid);
	bool LoadLaneMap(const string& path, Navigation::WalkableGrid& grid);
	
};