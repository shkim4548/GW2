#pragma once

// Asio 반드시 최상단
#define ASIO_STANDALONE
#ifdef _WIN32
#define _WIN32_WINNT 0x0601
#endif
#include "asio.hpp"

// 표준 라이브러리
#include <iostream>
#include <string>
#include <vector>
#include <list>
#include <queue>
#include <stack>
#include <map>
#include <set>
#include <unordered_map>
#include <unordered_set>
#include <array>
#include <memory>
#include <functional>
#include <thread>
#include <mutex>
#include <atomic>
#include <chrono>

using namespace std;

// 플랫폼별 최소 헤더
#ifdef _WIN32
#include <windows.h>
#endif

// 프로젝트 기반 헤더 (의존 순서 엄수)
#include "Types.h"
#include "Container.h"
#include "CoreMacro.h"
#include "CoreGlobal.h"
#include "CoreTLS.h"
#include "Lock.h"
#include "ObjectPool.h"
#include "LockQueue.h"
#include "JobTimer.h"
#include "JobQueue.h"