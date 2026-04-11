#pragma once

// Asio 반드시 최상단
#define ASIO_STANDALONE
#define _WIN32_WINNT 0x0601
#include "asio.hpp"

// 표준 라이브러리
#include <iostream>
#include <string>
#include <vector>
#include <memory>
#include <functional>
#include <thread>
#include <mutex>
#include <atomic>
#include <queue>
#include <set>

using namespace std;

// 플랫폼별 최소 헤더 (게임 로직에서 직접 쓰는 것만)
#ifdef _WIN32
#include <windows.h>   // HANDLE 등 아직 쓰는 곳이 있으면 유지
#endif
