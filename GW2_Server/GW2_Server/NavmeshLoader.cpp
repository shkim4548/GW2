#include "pch.h"
#include "NavmeshLoader.h"
#include <fstream>
#include <sstream>
#include <filesystem>
#include "NavigationSystem.h"
#include <nlohmann/json.hpp>
using json = nlohmann::json;

void NavmeshLoader::LoadObjFile(const char* filePath, OBJ_CollisionMesh& mesh)
{
    ifstream file(filePath);

    if (!file.is_open())
    {
        GConsoleLogger->WriteStdErr(Color::RED, L"Error : Cannot open Obj file\n");
        return;
    }

    string line;
    while (getline(file, line))
    {
        stringstream ss(line);
        string tag;
        ss >> tag;

        if (tag == "v")
        {
            GameMath::Vector3 v;
            ss >> v._x >> v._y >> v._z;
            mesh.Vertices.push_back(v);
        }
        else if (tag == "f")
        {
            // OBJ는 보통 f v/vt/vn 형식이므로 추가 파싱 필요
            string a, b, c;
            ss >> a >> b >> c;

            auto ToIndex = [&](const string& s)
                {
                    // "12/1/3" → 12 추출
                    stringstream sss(s);
                    int32 idx;
                    sss >> idx;
                    return idx - 1; // OBJ는 1-based
                };

            Triangle tri;
            tri.v1 = mesh.Vertices[ToIndex(a)];
            tri.v2 = mesh.Vertices[ToIndex(b)];
            tri.v3 = mesh.Vertices[ToIndex(c)];
            mesh.Triangles.push_back(tri);
        }
    }

    cout << "OBJ Loading Complete. Vertices: "
        << mesh.Vertices.size() << ", Faces: "
        << mesh.Triangles.size() << endl;
}

bool NavmeshLoader::LoadMeshBin(vector<Triangle>& outTriangles, Navigation::WalkableGrid& outGrid, const string& path)
{
    namespace fs = std::filesystem;

    ifstream in(path, ios::binary);
    if (!in.is_open())
    {
        fs::path cwd = fs::current_path();
        fs::path fullPath = fs::absolute(path);
        std::cout << "[CWD]      " << cwd.string() << std::endl;
        //std::cout << "[REL]      " << relativePath << std::endl;
        std::cout << "[ABSOLVE]  " << fullPath.string() << std::endl;
        std::cout << "[EXISTS]   " << (fs::exists(fullPath) ? "YES" : "NO") << std::endl;
        return false;
    }

    int32 triCount = 0;
    in.read((char*)&triCount, sizeof(int32));

    in.read((char*)&outGrid.width, sizeof(int32));
    in.read((char*)&outGrid.height, sizeof(int32));
    in.read((char*)&outGrid.cellSize, sizeof(float));
    in.read((char*)&outGrid.origin, sizeof(GameMath::Vector3));

    // Triangle 로드
    outTriangles.resize(triCount);
    in.read((char*)outTriangles.data(),
        sizeof(Triangle) * triCount);

    // Grid 로드
    int32 cellCount = outGrid.width * outGrid.height;
    outGrid.cells.resize(cellCount);
    in.read((char*)outGrid.cells.data(),
        sizeof(Navigation::GridCell) * cellCount);

    return true;
}

bool NavmeshLoader::LoadJsonMeshFile(const string& path, vector<GameMath::Vector3>& outVertices, vector<int32>& outIndices)
{
    std::ifstream in(path);
    if (!in.is_open())
    {
        std::cout << "[NavmeshJsonLoader] Failed to open: " << path << std::endl;
        return false;
    }

    json j;
    in >> j;

    // -------- Vertices --------
    const auto& jVertices = j["vertices"];
    outVertices.reserve(jVertices.size());

    for (const auto& v : jVertices)
    {
        GameMath::Vector3 pos;
        pos._x = v[0].get<float>();
        pos._y = v[1].get<float>();
        pos._z = v[2].get<float>();
        outVertices.push_back(pos);
    }

    // -------- Indices --------
    const auto& jIndices = j["indices"];
    outIndices.reserve(jIndices.size());

    for (const auto& idx : jIndices)
    {
        outIndices.push_back(idx.get<int32>());
    }

    // -------- Defensive checks --------
    if (outVertices.empty() || outIndices.empty() || (outIndices.size() % 3 != 0))
    {
        std::cout << "[NavmeshJsonLoader] Invalid navmesh data" << std::endl;
        return false;
    }

    std::cout << "[NavmeshJsonLoader] Loaded vertices: "
        << outVertices.size()
        << ", indices: "
        << outIndices.size() << std::endl;

    return true;
}

bool NavmeshLoader::LoadNavGridBin(const string& path, Navigation::WalkableGrid& OUT outGrid)
{
    std::ifstream in(path, std::ios::binary);
    if (!in.is_open())
        return false;

    Navigation::FileHeader header{};
    in.read(reinterpret_cast<char*>(&header), sizeof(header));

    if (header.magic != 0x4E524744) // 'NRGD'
        return false;

    if (header.version != 1)
        return false;

    // Grid meta
    in.read(reinterpret_cast<char*>(&outGrid.width), sizeof(int32));
    in.read(reinterpret_cast<char*>(&outGrid.height), sizeof(int32));
    in.read(reinterpret_cast<char*>(&outGrid.cellSize), sizeof(float));
    in.read(reinterpret_cast<char*>(&outGrid.origin), sizeof(GameMath::Vector3));

    const size_t cellCount =
        static_cast<size_t>(outGrid.width) * outGrid.height;

    outGrid.cells.resize(cellCount);

    // 핵심: 단 한 번에 읽는다
    in.read(reinterpret_cast<char*>(outGrid.cells.data()),
        sizeof(Navigation::GridCell) * cellCount);

    return true;
}
