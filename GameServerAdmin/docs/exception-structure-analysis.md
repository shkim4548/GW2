# Exception 구조 분석

> 대상 프로젝트: `GameServerAdmin`  
> 분석 범위: Common/Exceptions + ExceptionMiddleware + ErrorCode + Responses  
> 작성일: 2026-04-21

---

## 목차

1. [클래스 계층도](#1-클래스-계층도)
2. [HTTP 상태 코드 매핑](#2-http-상태-코드-매핑)
3. [각 예외 클래스 분석](#3-각-예외-클래스-분석)
   - [AppException](#31-appexception--추상-기반)
   - [NotFoundException](#32-notfoundexception)
   - [DomainException](#33-domainexception)
   - [InvalidStateException](#34-invalidstateexception)
   - [ForbiddenException / UnauthorizedException / BadRequestException](#35-forbiddenexception--unauthorizedexception--badrequestexception)
   - [RequestValidationException](#36-requestvalidationexception)
4. [도메인별 구체 예외](#4-도메인별-구체-예외)
5. [ErrorCode 열거형](#5-errorcode-열거형)
6. [응답 형식](#6-응답-형식)
7. [ExceptionMiddleware 처리 흐름](#7-exceptionmiddleware-처리-흐름)
8. [설계 패턴 — 정적 팩토리 메서드](#8-설계-패턴--정적-팩토리-메서드)
9. [주의 사항](#9-주의-사항)

---

## 1. 클래스 계층도

```
Exception (System)
└── AppException  (abstract)
    ├── NotFoundException                 → 404
    │   ├── PostNotFoundException         → 404  (Post/)
    │   ├── CommentNotFoundException      → 404
    │   ├── ParentCommentNotFoundException→ 404
    │   ├── PostNotFoundException         → 404  (Comment/) ⚠️ 버그 있음
    │   ├── NoticeNotFoundException       → 404
    │   └── PlayerNotFoundException       → 404
    │
    ├── DomainException                   → 409
    │   └── InvalidParentCommentException → 409
    │
    ├── InvalidStateException             → 409
    │   ├── InvalidCommentStateException  → 409
    │   └── InvalidNoticeStateException   → 409
    │
    ├── InvalidPostStateException         → 406  ⚠️ 다른 클래스와 다른 상태코드
    ├── ForbiddenException                → 403
    ├── UnauthorizedException             → 401
    └── BadRequestException               → 400

RequestValidationException (별도 계층)  → 400
    └── AppException을 상속하지만 처리 분기가 다름
```

> `RequestValidationException`은 `AppException`을 상속하지만,  
> ExceptionMiddleware에서 **별도 catch 블록**으로 먼저 처리된다.

---

## 2. HTTP 상태 코드 매핑

| 예외 클래스 | HTTP 상태 코드 | 의미 |
|------------|--------------|------|
| `BadRequestException` | 400 | 잘못된 요청 파라미터 |
| `RequestValidationException` | 400 | 요청 바디 필드 유효성 실패 |
| `UnauthorizedException` | 401 | 인증 없음 |
| `ForbiddenException` | 403 | 인증 있지만 권한 없음 |
| `NotFoundException` 계열 | 404 | 리소스 없음 |
| `InvalidPostStateException` | 406 | 게시글 상태 오류 |
| `DomainException` | 409 | 도메인 규칙 위반 |
| `InvalidStateException` 계열 | 409 | 리소스 상태 오류 |
| `Exception` (기타) | 500 | 알 수 없는 서버 오류 |

**설계 원칙**
- 400 계열: 클라이언트의 잘못된 요청
- 401/403: 인증/권한 문제
- 404: 존재하지 않는 리소스
- 409: 현재 상태에서 허용되지 않는 작업
- 500: 서버 내부 오류 (Unhandled)

---

## 3. 각 예외 클래스 분석

### 3.1 AppException — 추상 기반

```csharp
public abstract class AppException : Exception
{
    protected AppException(ErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
    public abstract int StatusCode { get; }
    public ErrorCode ErrorCode { get; }
}
```

**포인트**
- 모든 애플리케이션 예외의 뿌리
- `StatusCode`는 abstract → 하위 클래스가 반드시 HTTP 상태 코드 지정
- `ErrorCode`는 생성 시 고정 → 불변(immutable)
- ExceptionMiddleware는 이 타입을 기준으로 처리 분기

---

### 3.2 NotFoundException

```csharp
public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(ErrorCode.NOT_FOUND, message) { }

    protected NotFoundException(ErrorCode errorCode, string message)
        : base(errorCode, message) { }

    public override int StatusCode => StatusCodes.Status404NotFound;
}
```

**포인트**
- 생성자가 2개다
  - `public` 생성자: ErrorCode.NOT_FOUND 고정. 빠른 사용
  - `protected` 생성자: 도메인별 전용 ErrorCode 주입 허용 (하위 클래스 전용)
- 하위 클래스는 `protected` 생성자를 통해 더 구체적인 ErrorCode를 전달한다

```csharp
// 하위 클래스 패턴
public sealed class CommentNotFoundException : NotFoundException
{
    public CommentNotFoundException(long commentId)
        : base(ErrorCode.COMMENT_NOT_FOUND, $"댓글을 찾을 수 없습니다. commentId={commentId}")
    { }
}
```

클라이언트는 응답의 `errorCode` 필드로 "어떤 리소스가 없는지" 구분할 수 있다.

---

### 3.3 DomainException

```csharp
public class DomainException : AppException
{
    public DomainException(string message)
        : base(ErrorCode.VALIDATION_FAILED, message) { }

    public override int StatusCode => StatusCodes.Status409Conflict;
}
```

**포인트**
- ErrorCode는 `VALIDATION_FAILED`로 고정
- HTTP 409 반환 — "현재 상태에서 이 작업은 허용되지 않음"
- Domain 엔티티 내부에서 비즈니스 규칙 위반 시 직접 throw

```csharp
// Comment.cs 도메인 엔티티 내부 사용 예
if (string.IsNullOrWhiteSpace(content))
    throw new DomainException("댓글 내용을 입력해주세요.");
```

---

### 3.4 InvalidStateException

```csharp
public class InvalidStateException : AppException
{
    public InvalidStateException(string message)
        : base(ErrorCode.INVALID_STATE, message) { }

    public override int StatusCode => StatusCodes.Status409Conflict;
}
```

**DomainException과의 차이**

| 항목 | DomainException | InvalidStateException |
|------|----------------|----------------------|
| 발생 위치 | Domain 엔티티 내부 | Service 레이어 |
| 의미 | 도메인 규칙 위반 | 리소스의 현재 상태가 작업을 허용하지 않음 |
| 예시 | 빈 댓글 내용 | 이미 삭제된 댓글 수정 시도 |
| ErrorCode | VALIDATION_FAILED | INVALID_STATE |
| HTTP | 409 | 409 |

---

### 3.5 ForbiddenException / UnauthorizedException / BadRequestException

세 클래스 모두 동일한 구조다.

```csharp
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(ErrorCode.FORBIDDEN, message) { }
    public override int StatusCode => StatusCodes.Status403Forbidden;
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(ErrorCode.UNAUTHORIZED, message) { }
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message) : base(ErrorCode.BAD_REQUEST, message) { }
    public override int StatusCode => StatusCodes.Status400BadRequest;
}
```

**포인트**
- `UnauthorizedException`만 기본값(`"Unauthorized"`)이 있다 → 메시지 없이 throw 가능
- `ForbiddenException`은 본인 아닌 리소스 조작 시 Service에서 직접 throw
- `BadRequestException`은 개별 필드 오류가 아닌 전체 요청이 의미없을 때 사용

---

### 3.6 RequestValidationException

```csharp
public sealed class RequestValidationException : AppException
{
    public FieldErrorCollection FieldErrors { get; }

    public RequestValidationException(FieldErrorCollection fieldErrors)
        : base(ErrorCode.VALIDATION_FAILED, "요청 값이 올바르지 않습니다.")
    {
        FieldErrors = fieldErrors;
    }

    public override int StatusCode => StatusCodes.Status400BadRequest;
}
```

**FieldErrorCollection**

```csharp
public class FieldErrorCollection : Dictionary<string, List<string>>
{
    public void AddError(string field, string message)
    {
        if (!TryGetValue(field, out var errors))
        {
            errors = new List<string>();
            this[field] = errors;
        }
        errors.Add(message);
    }
}
```

**포인트**
- `Dictionary<string, List<string>>` 구조 → 한 필드에 여러 오류 메시지 가능
- `AddError("content", "내용을 입력해주세요.")` 방식으로 누적
- ExceptionMiddleware에서 **다른 catch 블록**으로 먼저 처리 → `ValidationErrorResponse` 반환

---

## 4. 도메인별 구체 예외

### Comment

| 클래스 | 상속 | ErrorCode | 설명 |
|--------|------|-----------|------|
| `CommentNotFoundException` | NotFoundException | COMMENT_NOT_FOUND | 댓글 미존재 |
| `ParentCommentNotFoundException` | NotFoundException | PARENT_COMMENT_NOT_FOUND | 부모 댓글 미존재 |
| `InvalidCommentStateException` | InvalidStateException | INVALID_STATE | 댓글 상태 오류 |
| `InvalidParentCommentException` | DomainException | VALIDATION_FAILED | 부모 댓글 규칙 위반 |

`InvalidCommentStateException`은 정적 팩토리 메서드를 제공한다.

```csharp
InvalidCommentStateException.AlreadyDeleted()  // 이미 삭제된 댓글
InvalidCommentStateException.NotDeleted()      // 삭제 안 된 댓글에 복구 시도
InvalidCommentStateException.CannotUpdate()    // 삭제된 댓글 수정 시도
```

`InvalidParentCommentException`도 마찬가지다.

```csharp
InvalidParentCommentException.NestedReplyNotAllowed(parentId)  // 대댓글에 대댓글
InvalidParentCommentException.ParentDeleted(parentId)          // 삭제된 댓글에 대댓글
```

### Post

| 클래스 | 상속 | HTTP | 설명 |
|--------|------|------|------|
| `PostNotFoundException` (Post/) | NotFoundException | 404 | 게시글 미존재 |
| `InvalidPostStateException` | AppException (직접) | **406** | 게시글 상태 오류 |

`InvalidPostStateException`은 `InvalidStateException`을 상속하지 않고 `AppException`을 직접 상속한다.  
결과적으로 HTTP 406(Not Acceptable)을 반환 — 다른 `InvalidState` 계열(409)과 다르다.

### Notice

| 클래스 | 상속 | 설명 |
|--------|------|------|
| `NoticeNotFoundException` | NotFoundException | 공지 미존재 |
| `InvalidNoticeStateException` | InvalidStateException | 공지 상태 오류 |

### Game

| 클래스 | 상속 | 설명 |
|--------|------|------|
| `PlayerNotFoundException` | NotFoundException | 플레이어 미존재 |

---

## 5. ErrorCode 열거형

```csharp
public enum ErrorCode
{
    // Common
    VALIDATION_FAILED, NOT_FOUND, FORBIDDEN, UNAUTHORIZED,
    INVALID_STATE, DOMAIN_RULE_VIOLATION, DUPLICATED_RESOURCE,

    // System
    INTERNAL_ERROR, SERVICE_UNAVAILABLE,

    // Board
    BOARD_POST_NOT_FOUND, BOARD_POST_FORBIDDEN,
    BOARD_POST_ALREADY_DELETED, BOARD_COMMENT_LIMIT_EXCEEDED,

    // Game
    GAME_PLAYER_NOT_FOUND, GAME_INSUFFICIENT_RESOURCE,
    GAME_INVALID_ACTION, GAME_COOLDOWN_NOT_EXPIRED,

    // Admin
    ADMIN_PERMISSION_REQUIRED,

    // Comment
    COMMENT_NOT_FOUND, PARENT_COMMENT_NOT_FOUND, INVALID_PARENT_COMMENT,
    COMMENT_ALREADY_DELETED, COMMENT_NOT_DELETED, COMMENT_UPDATE_FORBIDDEN,
    COMMENT_CONTENT_EMPTY, COMMENT_CONTENT_TOO_LONG,

    // Notice
    NOTICE_NOT_FOUND, NOTICE_ALREADY_PUBLISHED, NOTICE_ALREADY_DELETED,
    NOTICE_NOT_PUBLISHED, NOTICE_NOT_DELETED,

    // Bad Request
    BAD_REQUEST,
}
```

**설계 특징**
- 제네릭(`NOT_FOUND`)과 도메인별(`COMMENT_NOT_FOUND`) 코드 공존
- 클라이언트는 HTTP 상태 코드 외에 `errorCode`로 세분화된 처리 가능
- 일부 코드(`DOMAIN_RULE_VIOLATION`, `DUPLICATED_RESOURCE`, `BOARD_COMMENT_LIMIT_EXCEEDED`)는 현재 대응하는 예외 클래스가 없음 → 향후 확장 예정

---

## 6. 응답 형식

### 일반 오류 — ErrorResponse

```json
{
  "errorCode": "COMMENT_NOT_FOUND",
  "message": "댓글을 찾을 수 없습니다. commentId=42",
  "traceId": "0HMCK3TJ7PJIL:00000001"
}
```

```csharp
public class ErrorResponse
{
    public ErrorCode ErrorCode { get; set; }
    public string Message { get; set; }
    public string TraceId { get; set; }
}
```

### 유효성 오류 — ValidationErrorResponse

```json
{
  "errorCode": "VALIDATION_FAILED",
  "message": "요청 값이 올바르지 않습니다.",
  "traceId": "0HMCK3TJ7PJIL:00000001",
  "fieldErrors": {
    "content": ["내용을 입력해주세요.", "1000자를 초과했습니다."],
    "title": ["제목을 입력해주세요."]
  }
}
```

```csharp
public class ValidationErrorResponse : ErrorResponse
{
    public IDictionary<string, List<string>> FieldErrors { get; }
}
```

`ValidationErrorResponse`는 `ErrorResponse`를 상속하므로 공통 필드(`errorCode`, `message`, `traceId`)를 그대로 갖는다.

---

## 7. ExceptionMiddleware 처리 흐름

```
요청 처리 중 예외 발생
        │
        ▼
catch (RequestValidationException ex)
        │   ← 가장 먼저 처리
        │   StatusCode = 400
        │   응답: ValidationErrorResponse (FieldErrors 포함)
        │   로그: LogInformation
        │
catch (AppException ex)
        │   ← 두 번째
        │   StatusCode = ex.StatusCode  (예외 클래스가 결정)
        │   응답: ErrorResponse
        │   로그: LogWarning
        │
catch (Exception ex)
            ← 마지막 (예상치 못한 오류)
            StatusCode = 500
            응답: ErrorResponse (INTERNAL_ERROR, 고정 메시지)
            로그: LogError
```

**핵심 — StatusCode 결정 방식**

```csharp
// ExceptionMiddleware
context.Response.StatusCode = ex.StatusCode;  // 예외 객체에서 읽음

// AppException 하위 클래스
public override int StatusCode => StatusCodes.Status404NotFound;  // 각 클래스가 고정값 반환
```

Controller에서 상태 코드를 신경 쓸 필요가 없다.  
`throw new CommentNotFoundException(id)` 한 줄이면 미들웨어가 404 + JSON 응답까지 처리한다.

---

## 8. 설계 패턴 — 정적 팩토리 메서드

`InvalidCommentStateException`과 `InvalidParentCommentException`은 정적 팩토리 메서드를 제공한다.

```csharp
// 사용 측 (Service)
throw InvalidCommentStateException.AlreadyDeleted();
throw InvalidParentCommentException.NestedReplyNotAllowed(parentComment.Id);

// vs. 직접 생성
throw new InvalidCommentStateException("이미 삭제된 댓글입니다.");
```

**장점**
- 메시지 문자열을 Service에 노출하지 않아도 됨
- 같은 오류 상황에서 항상 동일한 메시지 보장
- 상황별 메서드 이름이 의도를 명확하게 표현

**언제 사용하는가**
- 오류 상황이 2가지 이상이고 상황마다 메시지가 다를 때
- 파라미터(ID 등)를 포함한 메시지가 필요할 때

---

## 9. 주의 사항

### ① InvalidPostStateException의 상태 코드 불일치

```csharp
// Post/InvalidPostStateException.cs
public sealed class InvalidPostStateException : AppException  // ← InvalidStateException 아님
{
    public override int StatusCode => StatusCodes.Status406NotAcceptable;  // ← 406
}

// 나머지 InvalidXxxStateException 계열
public override int StatusCode => StatusCodes.Status409Conflict;  // 409
```

`InvalidPostStateException`만 `InvalidStateException`을 상속하지 않고 `AppException`을 직접 상속하며  
406을 반환한다. 의도적인 구분인지 확인이 필요하다.

---

### ② Comment/PostNotFoundException 메시지 오류

`Common/Exceptions/Comment/PostNotFoundException.cs` 파일에 버그가 있다.

```csharp
// 현재 (잘못됨)
namespace GameServerAdmin.Common.Exceptions.Comment
{
    public sealed class PostNotFoundException : NotFoundException
    {
        public PostNotFoundException(long commentId)   // ← 파라미터명이 commentId
            : base(
                ErrorCode.BOARD_POST_NOT_FOUND,
                $"댓글을 찾을 수 없습니다. commentId={commentId}"  // ← "댓글"이라고 표현
            )
    }
}
```

- 파라미터명이 `commentId`이지만 이 클래스는 Post를 찾지 못했을 때 사용하는 예외
- 메시지가 "댓글을 찾을 수 없습니다"로 잘못 작성됨 (Post not found인데)
- `Post/PostNotFoundException.cs`가 별도로 존재하므로 이 파일은 사용 여부 확인 필요

**수정 방향**

```csharp
// 수정 후
public PostNotFoundException(long postId)
    : base(
        ErrorCode.BOARD_POST_NOT_FOUND,
        $"게시글을 찾을 수 없습니다. postId={postId}"
    )
```
