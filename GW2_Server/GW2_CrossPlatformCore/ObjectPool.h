#pragma once

/*--------------
	ObjectPool
----------------*/

/*
	cross platform 용 단순한 버전
	원본의 memoryPool, StompAllocator 대신 new/delete를 사용
	인터페이스는 동일하게 유지
*/

template<typename Type>
class ObjectPool
{
public:
	template<typename... Args>
	static Type* Pop(Args&&... args)
	{
		return new Type(forward<Args>(args)...);
	}

	static void Push(Type* obj)
	{
		delete obj;
	}

	template<typename... Args>
	static shared_ptr<Type> MakeShared(Args&&... args)
	{
		shared_ptr<Type> ptr = { Pop(forward<Args>(args)...), Push };
		return ptr;
	}
};

