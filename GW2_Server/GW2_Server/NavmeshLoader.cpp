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

    // -----------------------------
    // 1. File Header
    // -----------------------------
    uint32 magic = 0;
    uint32 version = 0;

    in.read(reinterpret_cast<char*>(&magic), sizeof(uint32));
    in.read(reinterpret_cast<char*>(&version), sizeof(uint32));

    if (!in || magic != 0x4E524744) // 'NRGD'
        return false;

    if (version != 1)
        return false;

    // -----------------------------
    // 2. Grid Meta
    // -----------------------------
    int32 width = 0;
    int32 height = 0;
    float cellSize = 0.0f;
    float ox = 0.0f, oy = 0.0f, oz = 0.0f;

    in.read(reinterpret_cast<char*>(&width), sizeof(int32));
    in.read(reinterpret_cast<char*>(&height), sizeof(int32));
    in.read(reinterpret_cast<char*>(&cellSize), sizeof(float));
    in.read(reinterpret_cast<char*>(&ox), sizeof(float));
    in.read(reinterpret_cast<char*>(&oy), sizeof(float));
    in.read(reinterpret_cast<char*>(&oz), sizeof(float));

    if (!in || width <= 0 || height <= 0)
        return false;

    outGrid.width = width;
    outGrid.height = height;
    outGrid.cellSize = cellSize;
    outGrid.origin = GameMath::Vector3{ ox, oy, oz };

    const size_t cellCount =
        static_cast<size_t>(width) * static_cast<size_t>(height);

    outGrid.cells.clear();
    outGrid.cells.resize(cellCount);

    // -----------------------------
    // 3. Cell Data (walkable only)
    // -----------------------------
    for (size_t i = 0; i < cellCount; ++i)
    {
        uint8 walkable = 0;
        in.read(reinterpret_cast<char*>(&walkable), sizeof(uint8));

        if (!in)
            return false;

        outGrid.cells[i].walkable = (walkable != 0);
    }

    return true;
}

bool NavmeshLoader::LoadLaneMap(const string& path, Navigation::WalkableGrid& grid)
{
    ifstream ifs(path, ios::binary);
    if (!ifs.is_open())
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneMapLoader] Failed to open file: %S\n", path.c_str());
        return false;
    }

    LaneMapHeader header{};
    ifs.read(reinterpret_cast<char*>(&header), sizeof(header));
    if (!ifs)
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneMapLoader] Failed to read header: %S\n", path.c_str());
        return false;
    }

    if (header.magic != 0x4C4E4D50) // 'LNMP'
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneMapLoader] Invalid magic in %S\n", path.c_str());
        return false;
    }

    if (header.version != 1)
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneMapLoader] Unsupported version(%d) in %S\n", header.version, path.c_str());
        return false;
    }

    if (header.width != grid.width || header.height != grid.height)
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneMapLoader] Size mismatch. laneMap=(%d,%d) grid=(%d,%d)\n",
            header.width, header.height, grid.width, grid.height);
        return false;
    }

    const size_t cellCount = static_cast<size_t>(grid.width) * grid.height;
    vector<uint8> laneBuf(cellCount);
    ifs.read(reinterpret_cast<char*>(laneBuf.data()), laneBuf.size());
    if (!ifs)
    {
        GConsoleLogger->WriteStdErr(Color::RED,
            L"[LaneMapLoader] Failed to read lane data: %S\n", path.c_str());
        return false;
    }

    // laneId 적용 (row-major: idx = z * width + x)
    for (int32 z = 0; z < grid.height; ++z)
    {
        for (int32 x = 0; x < grid.width; ++x)
        {
            size_t idx = static_cast<size_t>(z) * grid.width + x;
            Navigation::GridCell& cell = grid.At(x, z);
            cell.laneId = laneBuf[idx];
        }
    }

    GConsoleLogger->WriteStdOut(Color::GREEN,
        L"[LaneMapLoader] Loaded laneMap from %S\n", path.c_str());
    return true;
}
