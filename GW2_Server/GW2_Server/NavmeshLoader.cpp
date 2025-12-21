#include "pch.h"
#include "NavmeshLoader.h"
#include <fstream>
#include <sstream>

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
            Vector3 v;
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
