#pragma once

// MSVC SAL 미지원 환경 대응
#ifdef _MSC_VER
#define __analysis_assume(expr)
#endif

#define OUT
#define SETTER

#define NAMESPACE_BEGIN(name) namespace name{
#define NAMESPACE_END			}

/*---------------
	  Lock
---------------*/

#define USE_MANY_LOCKS(count)	Lock _locks[count];
#define USE_LOCK				USE_MANY_LOCKS(1)
#define	READ_LOCK_IDX(idx)		ReadLockGuard readLockGuard_##idx(_locks[idx], typeid(this).name());
#define READ_LOCK				READ_LOCK_IDX(0)
#define	WRITE_LOCK_IDX(idx)		WriteLockGuard writeLockGuard_##idx(_locks[idx], typeid(this).name());
#define WRITE_LOCK				WRITE_LOCK_IDX(0)

/*---------------
	  Crash
---------------*/

#define CRASH(cause)						\
{											\
	uint32* crash = nullptr;				\
	__analysis_assume(crash != nullptr);	\
	*crash = 0xDEADBEEF;					\
}

#define ASSERT_CRASH(expr)			\
{									\
	if (!(expr))					\
	{								\
		CRASH("ASSERT_CRASH");		\
		__analysis_assume(expr);	\
	}								\
}

// 크로스플랫폼 GetTickCount64 대체
inline uint64 GetCurrentTick()
{
    using namespace chrono;
    return static_cast<uint64>(
        duration_cast<milliseconds>(
            steady_clock::now().time_since_epoch()
        ).count()
        );
}