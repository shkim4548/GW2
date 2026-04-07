#pragma once
#include "DBConnection.h"

/*---------------------
	DBConnectionPool
-----------------------*/

class DBConnectionPool
{
public:
	DBConnectionPool();
	~DBConnectionPool();

	bool			Connect(int32 connectionCount, const WCHAR* connectionString);	// 몇개를 어떤 DB와 연결한가
	void			Clear();

	DBConnection*	 Pop();	// 생포인터로 해도 된다. 일회성 작업 후 바로 반납
	void			 Push(DBConnection* connection);

private:
	USE_LOCK;
	SQLHENV					_environment;
	Vector<DBConnection*>	_connections;
};

