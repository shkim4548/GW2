#include "pch.h"
#include "DBConnection.h"

bool DBConnection::Connect(SQLHENV henv, const WCHAR* connectionString)
{
    // 연결자체에 실패한 경우
    if (::SQLAllocHandle(SQL_HANDLE_DBC, henv, &_connection) != SQL_SUCCESS)
        return false;

    WCHAR stringBuffer[MAX_PATH] = { 0 };
    ::wcscpy_s(stringBuffer, connectionString);

    WCHAR resultString[MAX_PATH] = { 0 };
    SQLSMALLINT resultStringLen = 0;
    
    // 실질적인 DB 연결 부분
    SQLRETURN ret = ::SQLDriverConnectW(
        _connection,
        NULL,
        reinterpret_cast<SQLWCHAR*>(stringBuffer),
        _countof(stringBuffer),
        OUT reinterpret_cast<SQLWCHAR*>(resultString),
        _countof(resultString),
        OUT & resultStringLen,
        SQL_DRIVER_NOPROMPT
    );

    if (::SQLAllocHandle(SQL_HANDLE_STMT, _connection, &_statement) != SQL_SUCCESS)
        return false;

    // 연결에 성공한 경우
    return(ret == SQL_SUCCESS || ret == SQL_SUCCESS_WITH_INFO);
}

void DBConnection::Clear()
{
    if (_connection != SQL_NULL_HANDLE)
    {
        ::SQLFreeHandle(SQL_HANDLE_DBC, _connection);
        _connection = SQL_NULL_HANDLE;
    }    
    
    if (_statement != SQL_NULL_HANDLE)
    {
        ::SQLFreeHandle(SQL_HANDLE_STMT, _statement);
        _statement = SQL_NULL_HANDLE;
    }
}

// SQL 쿼리를 받아서 실행하라고 던진다.
bool DBConnection::Execute(const WCHAR* query)
{
    SQLRETURN ret = ::SQLExecDirectW(_statement, (SQLWCHAR*)query, SQL_NTSL);
    if (ret == SQL_SUCCESS || ret == SQL_SUCCESS_WITH_INFO)
        return true;

    HandleError(ret);
    return false;
}

// 데이터를 긁어 올 때 사용하는 함수
bool DBConnection::Fetch()
{
    SQLRETURN ret = ::SQLFetch(_statement);

    // 이유를 체크해서 에러 여부를 판단한다.
    switch (ret)
    {
    case SQL_SUCCESS:
    case SQL_SUCCESS_WITH_INFO:
        return true;
    case SQL_NO_DATA:
        return false;
    case SQL_ERROR: // 무언가 요청한 것에서 잘못됨
        HandleError(ret);
        return false;
    default:
        return true;
    }
}

// 데이터가 몇개 있는지를 확인하는 함수
int32 DBConnection::GetRowCount()
{
    SQLLEN count = 0;
    SQLRETURN ret = ::SQLRowCount(_statement, OUT & count);

    if (ret == SQL_SUCCESS || ret == SQL_SUCCESS_WITH_INFO)
        return static_cast<int32>(count);

    return -1;  // -1이면 없다 -> 잘못되었다.
}

// BindParam, BindCol로 받아온 인자를 넘기기도 하고, 결과를 받기도 할 예정
void DBConnection::Unbind()
{
    // 우선 이전에 들어온 내용이 있다면 모두 없애서 초기화
    ::SQLFreeStmt(_statement, SQL_UNBIND);
    ::SQLFreeStmt(_statement, SQL_RESET_PARAMS);
    ::SQLFreeStmt(_statement, SQL_CLOSE);
}

bool DBConnection::BindParam(int32 paramIndex, bool* value, SQLLEN* index)
{
    return BindParam(paramIndex, SQL_C_TINYINT, SQL_C_TINYINT, size32(bool), value, index);
}

// size는 정수가 아닌경우 0으로 밀어줘도 상관 없다.
bool DBConnection::BindParam(int32 paramIndex, float* value, SQLLEN* index)
{
    return BindParam(paramIndex, SQL_C_FLOAT, SQL_REAL, 0, value, index);
}

bool DBConnection::BindParam(int32 paramIndex, double* value, SQLLEN* index)
{
    return BindParam(paramIndex, SQL_C_DOUBLE, SQL_DOUBLE, 0, value, index);
}

bool DBConnection::BindParam(int32 paramIndex, int8* value, SQLLEN* index)
{
    return BindParam(paramIndex, SQL_C_TINYINT, SQL_TINYINT, size32(int8), value, index);
}

bool DBConnection::BindParam(int32 paramIndex, int16* value, SQLLEN* index)
{
    return BindParam(paramIndex, SQL_C_SHORT, SQL_SMALLINT, size32(int16), value, index);
}

bool DBConnection::BindParam(int32 paramIndex, int32* value, SQLLEN* index)
{
    return BindParam(paramIndex, SQL_C_LONG, SQL_INTEGER, size32(int32), value, index);
}

bool DBConnection::BindParam(int32 paramIndex, int64* value, SQLLEN* index)
{
    return BindParam(paramIndex, SQL_C_SBIGINT, SQL_BIGINT, size32(int64), value, index);
}

bool DBConnection::BindParam(int32 paramIndex, TIMESTAMP_STRUCT* value, SQLLEN* index)
{
    return BindParam(paramIndex, SQL_C_TYPE_TIMESTAMP, SQL_C_TYPE_TIMESTAMP, size32(TIMESTAMP_STRUCT), value, index);
}

// 여기부터는 바이트의 크기를 받아와야한다.
bool DBConnection::BindParam(int32 paramIndex, const WCHAR* str, SQLLEN* index)
{
    SQLULEN size = static_cast<SQLULEN>((::wcslen(str) + 1) * 2);
    *index = SQL_NTSL;

    if (size > 4000)   // 테스트값이 4000인 이유는 일반적인 상황에서 가장 기준이 되는 값이기 때문이다.
        return BindParam(paramIndex, SQL_C_WCHAR, SQL_WLONGVARCHAR, size, (SQLPOINTER)str, index);
    // 너무 길어진 경우 SQL_WLONGVARCHAR로 받는다.
    else
        return BindParam(paramIndex, SQL_C_WCHAR, SQL_WVARCHAR, size, (SQLPOINTER)str, index);
    // 적정길이이므로 SQL_WVARCHAR로 받는다
}

bool DBConnection::BindParam(int32 paramIndex, const BYTE* bin, int32 size, SQLLEN* index)
{
    // 일반적인 이미지 등의 바이너리 파일을 저장할 때 사용한다.
    if (bin == nullptr)
    {
        *index = SQL_NULL_DATA;
        size = 1;
    }
    else
        *index = size;

    if(size>BINARY_MAX)
        return BindParam(paramIndex, SQL_C_BINARY, SQL_LONGVARBINARY, size, (BYTE*)bin, index);
    else
        return BindParam(paramIndex, SQL_C_BINARY, SQL_BINARY, size, (BYTE*)bin, index);

}

// 여기부터는 BindCol 함수이다.
bool DBConnection::BindCol(int32 columnIndex, bool* value, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_TINYINT, size32(bool), value, index);
}

bool DBConnection::BindCol(int32 columnIndex, float* value, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_FLOAT, size32(float), value, index);
}

bool DBConnection::BindCol(int32 columnIndex, double* value, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_DOUBLE, size32(double), value, index);
}

bool DBConnection::BindCol(int32 columnIndex, int8* value, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_TINYINT, size32(int16), value, index);
}

bool DBConnection::BindCol(int32 columnIndex, int16* value, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_SHORT, size32(int16), value, index);
}

bool DBConnection::BindCol(int32 columnIndex, int32* value, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_LONG, size32(int32), value, index);
}

bool DBConnection::BindCol(int32 columnIndex, int64* value, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_SBIGINT, size32(int64), value, index);
}

bool DBConnection::BindCol(int32 columnIndex, TIMESTAMP_STRUCT* value, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_TYPE_TIMESTAMP, size32(TIMESTAMP_STRUCT), value, index);
}

bool DBConnection::BindCol(int32 columnIndex, WCHAR* str, int32 size, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_C_WCHAR, size, str, index);
}

bool DBConnection::BindCol(int32 columnIndex, BYTE* bin, int32 size, SQLLEN* index)
{
    return BindCol(columnIndex, SQL_BINARY, size, bin, index);
}


bool DBConnection::BindParam(SQLUSMALLINT paramIndex, SQLSMALLINT cType, SQLSMALLINT sqlType, SQLULEN len, SQLPOINTER ptr, SQLLEN* index)
{
    // 매번마다 호출하여 statement를 이용해 n번째 인자를 어떻게 준비시킬지 설정
    SQLRETURN ret = ::SQLBindParameter(_statement, paramIndex, SQL_PARAM_INPUT, cType, sqlType, len, 0, ptr, 0, index);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO)
    {
        HandleError(ret);
        return false;
    }
    return true;
}

bool DBConnection::BindCol(SQLUSMALLINT columnIndex, SQLSMALLINT cType, SQLULEN len, SQLPOINTER value, SQLLEN* index)
{
    SQLRETURN ret = ::SQLBindCol(_statement, columnIndex, cType, value, len, index);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO)
    {
        HandleError(ret);
        return false;
    }
    return true;
}

// 연결 오류등 공통적인 오류들을 처리하기 위한 내용
void DBConnection::HandleError(SQLRETURN ret)
{
    // 성공했는데 여기 오면 에러다
    if (ret == SQL_SUCCESS)
        return;

    SQLSMALLINT index = 1;
    SQLWCHAR sqlState[MAX_PATH] = { 0 };
    SQLINTEGER nativeErr = 0;
    SQLWCHAR errMsg[MAX_PATH] = { 0 };  // 실질적인 문제가 뭔지 출력해준다.
    SQLSMALLINT msgLen = 0;
    SQLRETURN errorRet = 0;

    while (true)
    {
        errorRet = ::SQLGetDiagRecW(
            SQL_HANDLE_STMT,
            _statement,
            index,
            sqlState,
            OUT & nativeErr,
            errMsg,
            _countof(errMsg),
            OUT &msgLen
        );

        // 에러가 없는 상황
        if (errorRet == SQL_NO_DATA)
            break;

        // 에러가 없는 상황 2
        if (errorRet != SQL_SUCCESS && errorRet != SQL_SUCCESS_WITH_INFO)
            break;

        // TODO : Log, 라이브 서버에서는 로그를 파일로 남겨야한다. 여긴 장난식
        wcout.imbue(locale("kor")); // 한국어 사용을 염두에 둔 로그 출력
        wcout << errMsg << endl;

        index++;    // 인덱스를 증가 시켜 에러 메시지를 모두 순환
    }
}
