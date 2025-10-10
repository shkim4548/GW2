#pragma once
#include <sql.h>
#include <sqlext.h>

/*-----------------
	DBConnection
-------------------*/

enum
{
	WVARCHAR_MAX = 4000,
	BINARY_MAX = 8000
};

class DBConnection
{
public:
	bool		Connect(SQLHENV henv, const WCHAR* connectionString);
	void		Clear();

	bool		Execute(const WCHAR* query);
	bool		Fetch();	// Select 등의 쿼리에서 뭘 가져올건지 정해주는 함수
	int32		GetRowCount();
	void		Unbind();

public:
	bool		BindParam(int32 paramIndex, bool* value, SQLLEN* index);
	bool		BindParam(int32 paramIndex, float* value, SQLLEN* index);
	bool		BindParam(int32 paramIndex, double* value, SQLLEN* index);
	bool		BindParam(int32 paramIndex, int8* value, SQLLEN* index);
	bool		BindParam(int32 paramIndex, int16* value, SQLLEN* index);
	bool		BindParam(int32 paramIndex, int32* value, SQLLEN* index);
	bool		BindParam(int32 paramIndex, int64* value, SQLLEN* index);
	bool		BindParam(int32 paramIndex, TIMESTAMP_STRUCT* value, SQLLEN* index);
	bool		BindParam(int32 paramIndex, const WCHAR* str, SQLLEN* index);
	bool		BindParam(int32 paramIndex, const BYTE* bin, int32 size, SQLLEN* index);

	bool		BindCol(int32 columnIndex, bool* value, SQLLEN* index);
	bool		BindCol(int32 columnIndex, float* value, SQLLEN* index);
	bool		BindCol(int32 columnIndex, double* value, SQLLEN* index);
	bool		BindCol(int32 columnIndex, int8* value, SQLLEN* index);
	bool		BindCol(int32 columnIndex, int16* value, SQLLEN* index);
	bool		BindCol(int32 columnIndex, int32* value, SQLLEN* index);
	bool		BindCol(int32 columnIndex, int64* value, SQLLEN* index);
	bool		BindCol(int32 columnIndex, TIMESTAMP_STRUCT* value, SQLLEN* index);
	bool		BindCol(int32 columnIndex, WCHAR* str, int32 size, SQLLEN* index);
	bool		BindCol(int32 columnIndex, BYTE* bin, int32 size, SQLLEN* index);

	// SQL Server는 unsinged 데이터를 지원하지 않는다. 그래서 여긴 구현하지 않을 예정
	// 참고로 bool타입도 SQL Server에는 없다.

private:
	bool		BindParam(SQLUSMALLINT paramIndex, SQLSMALLINT cType, SQLSMALLINT sqlType, SQLULEN len, SQLPOINTER ptr, SQLLEN* index);
	bool		BindCol(SQLUSMALLINT columnIndex, SQLSMALLINT cType, SQLULEN len, SQLPOINTER value, SQLLEN* index);
	void		HandleError(SQLRETURN ret);

private:
	
	SQLHDBC		_connection = SQL_NULL_HANDLE;
	SQLHSTMT	_statement = SQL_NULL_HANDLE;	// 연결 후 요청을 보낼때 사용할 state

};

