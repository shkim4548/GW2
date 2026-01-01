// NavigationBaker.cpp : 이 파일에는 'main' 함수가 포함됩니다. 거기서 프로그램 실행이 시작되고 종료됩니다.
//

#include <iostream>
#include "Types.h"
#include <vector>
#include <fstream>
#include "ToolMath.h"
#include "NavmeshLoader.h"
#include "NavmeshMaker.h"
using namespace std;

unique_ptr<class NavmeshLoader> _navmeshLoader;
shared_ptr<struct OBJ_CollisionMesh> _collisionMesh;

void SaveNavBinary(const vector<Triangle>& triangles, const Navigation::WalkableGrid& grid, const string& path)
{
    ofstream out(path, ios::binary);
    if (!out.is_open())
        return;

    int32 triCount = (int32)triangles.size();

    out.write((char*)&triCount, sizeof(int32));
    out.write((char*)&grid.width, sizeof(int32));
    out.write((char*)&grid.height, sizeof(int32));
    out.write((char*)&grid.cellSize, sizeof(float));
    out.write((char*)&grid.origin, sizeof(GameMath::Vector3));

    // 삼각형
    out.write((char*)triangles.data(),
        sizeof(Triangle) * triangles.size());

    // Grid
    out.write((char*)grid.cells.data(),
        sizeof(Navigation::GridCell) * grid.cells.size());

    out.close();
}

bool LoadNavBinary(const string& path, vector<Triangle>& outTriangles, Navigation::WalkableGrid& outGrid)
{
    ifstream in(path, ios::binary);
    if (!in.is_open())
        return false;

    int32 triCount;

    in.read((char*)&triCount, sizeof(int32));
    in.read((char*)&outGrid.width, sizeof(int32));
    in.read((char*)&outGrid.height, sizeof(int32));
    in.read((char*)&outGrid.cellSize, sizeof(float));
    in.read((char*)&outGrid.origin, sizeof(GameMath::Vector3));

    outTriangles.resize(triCount);
    outGrid.cells.resize(outGrid.width * outGrid.height);

    in.read((char*)outTriangles.data(),
        sizeof(Triangle) * triCount);

    in.read((char*)outGrid.cells.data(),
        sizeof(Navigation::GridCell) * outGrid.cells.size());

    in.close();
    return true;
}


int main()
{
    cout << "Navigation now Backing!" << endl;
    shared_ptr<OBJ_CollisionMesh> mesh = make_shared<OBJ_CollisionMesh>();
    shared_ptr<NavmeshLoader> loader = make_shared<NavmeshLoader>();
    loader->LoadObjFile("../../GW2_Client/Assets/NavMeshExport/navmesh_collision.obj", *mesh);
    
    vector<GameMath::Vector3> vertices;
    vector<int32> indices;
    Navigation::NavigationSystem navSystem;
    // 2. Triangle 생성
    navSystem.Build(vertices, indices);
    const float cellSize = 0.5f;
    // 3. Grid 생성
    navSystem.BuildGrid(cellSize);

    // 4. Walkable Grid 생성
    Navigation::WalkableGrid grid;
    navSystem.BuildWalkableGrid(
        grid,
        navSystem.GetGroundVertical(),
        navSystem.GetGroundWidth(),
        cellSize,
        navSystem.GetGridOrigin()
    );

    // 5. Binary 저장
    //SaveNavBinary(grid);
    SaveNavBinary(
        navSystem.GetAllTriangles(),  // const vector<Triangle>&
        grid,
        "map.navbin"
    );
}