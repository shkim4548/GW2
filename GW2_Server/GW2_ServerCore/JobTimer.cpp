#include "pch.h"
#include "JobTimer.h"
#include "JobQueue.h"

/*-------------
	JobTimer
---------------*/

void JobTimer::Reserve(uint64 tickAfter, weak_ptr<JobQueue> owner, JobRef job)
{
	const uint64 executeTick = ::GetTickCount64() + tickAfter;
	JobData* jobData = ObjectPool<JobData>::Pop(owner, job);

	WRITE_LOCK;	// 전역으로 사용할 예정이므로 락을 건다.

	_items.push(TimerItem{ executeTick, jobData });
}

// 여기가 핵심이다.
void JobTimer::Distribute(uint64 now)
{
	// 1회에 1쓰레드만 통과시킨다
	if (_distributing.exchange(true) == true)	// 누군가가 이미 함수를 실행중
		return;

	// 최대한 빠르게 위의 함수에서 푸시된 아이템을 써버린다.
	Vector<TimerItem> items;
	
	{
		WRITE_LOCK;
		while (_items.empty() == false)
		{
			const TimerItem& timerItem = _items.top();
			if (now < timerItem.executeTick)
				break;

			items.push_back(timerItem);
			_items.pop();
		}
	}

	for (TimerItem& item : items)
	{
		if (JobQueueRef owner = item.jobData->owner.lock())	// 여기서 null 체크를 한 것이다.
			owner->Push(item.jobData->job);

		ObjectPool<JobData>::Push(item.jobData);
	}

	// 끝났으면 풀어준다
	_distributing.store(false);
}

void JobTimer::Clear()
{
	WRITE_LOCK;
	while (_items.empty() == false)
	{
		const TimerItem& timerItem = _items.top();
		ObjectPool<JobData>::Push(timerItem.jobData);
		_items.pop();
	}
}
