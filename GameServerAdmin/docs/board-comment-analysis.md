# 게시판 Comment 기능 분석

> 대상 프로젝트: `GameServerAdmin`  
> 분석 범위: Domain → Service → Controller  
> 작성일: 2026-04-21

---

## 목차

1. [전체 구조 한눈에 보기](#1-전체-구조-한눈에-보기)
2. [Domain 계층](#2-domain-계층)
   - [CommentStatus](#21-commentstatus)
   - [Comment](#22-comment)
3. [Service 계층](#3-service-계층)
   - [AdminCommentService](#31-admincommentservice)
   - [PublicCommentService](#32-publiccommentservice)
   - [두 서비스 비교](#33-두-서비스-비교)
4. [Controller 계층](#4-controller-계층)
   - [AdminCommentController](#41-admincommentcontroller)
   - [BoardCommentController](#42-boardcommentcontroller)
5. [Post와의 차이점](#5-post와의-차이점)
6. [미구현 항목 및 버그](#6-미구현-항목-및-버그)

---

## 1. 전체 구조 한눈에 보기

```
[Client / Admin Tool]
        │
        ▼
┌───────────────────────────────────────────────────┐
│                 Controller 계층                    │
│  BoardCommentController    AdminCommentController  │
│  (Razor, Form POST)         (JSON REST API)        │
└───────────────────────────────────────────────────┘
        │                           │
        ▼                           ▼
┌───────────────────────────────────────────────────┐
│                 Service 계층                       │
│  PublicCommentService      AdminCommentService     │
│  (소유권 검증, 계층 조립)    (관리자 전용 기능)       │
└───────────────────────────────────────────────────┘
        │                           │
        └──────────────┬────────────┘
                       ▼
┌───────────────────────────────────────────────────┐
│              Domain 계층 (공통)                    │
│  Comment  /  CommentStatus                        │
└───────────────────────────────────────────────────┘
```

**댓글 계층 구조 (1단계 제한)**

```
Post
 └─ Comment (원댓글, ParentCommentId = null)
     └─ Comment (대댓글, ParentCommentId = 원댓글 Id)
         └─ ❌ 대대댓글 불가 (도메인에서 차단)
```

---

## 2. Domain 계층

### 2.1 CommentStatus

```csharp
public enum CommentStatus
{
    Active,
    Deleted
}
```

**PostStatus와의 차이**

```csharp
// PostStatus — 값 명시
public enum PostStatus  { Active = 0, Deleted = 1 }

// CommentStatus — 값 미명시
public enum CommentStatus { Active, Deleted }
```

`CommentStatus`는 값을 명시하지 않아 컴파일러가 `Active = 0, Deleted = 1`을 자동 부여한다.  
현재는 문제없지만, 나중에 enum 순서가 바뀔 경우 DB 데이터와 불일치가 생길 수 있다.  
→ `PostStatus`처럼 값을 명시하는 것이 더 안전한 패턴이다.

---

### 2.2 Comment

#### 생성자 — 원댓글과 대댓글이 분리되어 있다

```csharp
// 원댓글
public Comment(long postId, long authorId, string content)
{
    PostId          = postId;
    ParentCommentId = null;   // null = 원댓글
    ...
}

// 대댓글
public Comment(long postId, long parentCommentId, long authorId, string content)
{
    PostId          = postId;
    ParentCommentId = parentCommentId;  // 값 있음 = 대댓글
    ...
}
```

`ParentCommentId`가 `null`이면 원댓글, 값이 있으면 대댓글이다.  
생성자를 분리해서 **대댓글을 만들 때는 반드시 `parentCommentId`를 넘기도록** 강제한다.

---

#### 상태 변경 메서드

**UpdateContent()**

```csharp
public void UpdateContent(string newContent)
{
    if (Status == CommentStatus.Deleted)
        throw InvalidCommentStateException.CannotUpdate();

    ValidateContent(newContent);
    Content   = newContent;
    UpdatedAt = DateTime.UtcNow;
}
```

**SoftDelete() / Restore()**

```csharp
public void SoftDelete()
{
    if (Status == CommentStatus.Deleted)
        throw InvalidCommentStateException.AlreadyDeleted();

    Status    = CommentStatus.Deleted;
    DeletedAt = DateTime.UtcNow;
}

public void Restore()
{
    if (Status != CommentStatus.Deleted)
        throw InvalidCommentStateException.NotDeleted();

    Status    = CommentStatus.Active;
    DeletedAt = null;
}
```

**Post와 예외 처리 방식이 다르다**

| 구분 | Post | Comment |
|------|------|---------|
| 예외 생성 방식 | `throw new InvalidPostStateException("메시지")` | `throw InvalidCommentStateException.AlreadyDeleted()` |
| 패턴 | 생성자에 메시지 직접 전달 | **정적 팩토리 메서드** 패턴 |

`InvalidCommentStateException.AlreadyDeleted()`처럼 정적 팩토리 메서드를 쓰면  
예외 메시지가 Exception 클래스 안에 캡슐화된다. 호출하는 쪽에서 메시지 문자열을 신경 쓰지 않아도 된다.

---

#### 헬퍼 메서드

```csharp
public bool IsReply()    => ParentCommentId.HasValue;
public bool IsAuthor(long authorId) => AuthorId == authorId;
```

- `IsReply()` — 대댓글 여부. Service에서 원댓글과 대댓글 분기 처리에 사용
- `IsAuthor()` — 작성자 여부. Service에서 소유권 검증에 사용

**도메인이 판단 로직을 보유하는 예시다.**  
Service가 `if (comment.AuthorId == authorId)` 직접 비교하지 않고 `comment.IsAuthor(authorId)`를 호출한다.

---

#### 유효성 검사

```csharp
private const int MaxContentLength = 1000;

private void ValidateContent(string content)
{
    if (string.IsNullOrWhiteSpace(content))
        throw new ArgumentException("댓글 내용은 비어있을 수 없습니다.", nameof(content));

    if (content.Length > MaxContentLength)
        throw new ArgumentException($"댓글 내용은 {MaxContentLength}자를 초과할 수 없습니다.", nameof(content));
}
```

Post와 달리 `DomainException` 대신 `ArgumentException`을 사용한다.  
두 방식의 차이:

| 구분 | DomainException | ArgumentException |
|------|----------------|-------------------|
| 의미 | 비즈니스 규칙 위반 | 잘못된 인자 전달 |
| 캐치 위치 | 전역 예외 핸들러 | 전역 예외 핸들러 또는 호출부 |

---

## 3. Service 계층

### 3.1 AdminCommentService

**인터페이스**

```csharp
public interface IAdminCommentService
{
    Task<PagedResponse<AdminCommentListItemDto>> GetAllCommentsAsync(AdminCommentListQuery query);
    Task<AdminCommentResponse> GetCommentDetailAsync(int commentId);
    Task DeleteCommentAsync(int commentId);
    Task RestoreCommentAsync(int commentId);
    Task HardDeleteCommentAsync(int commentId);
}
```

**GetAllCommentsAsync — 다중 필터 + 정렬**

```csharp
// 필터
if (query.PostId.HasValue)   queryable = queryable.Where(c => c.PostId   == query.PostId);
if (query.AuthorId.HasValue) queryable = queryable.Where(c => c.AuthorId == query.AuthorId.Value);
if (query.Status.HasValue)   queryable = queryable.Where(c => c.Status   == query.Status.Value);
if (!query.IncludeDeleted)   queryable = queryable.Where(c => c.Status   == CommentStatus.Active);

// 정렬 (switch expression)
queryable = query.SortBy.ToLower() switch
{
    "updatedat" => query.SortOrder == "asc" ? queryable.OrderBy(c => c.UpdatedAt) : ...,
    "deletedat" => ...,
    _           => query.SortOrder == "asc" ? queryable.OrderBy(c => c.CreatedAt) : ...
};
```

`query.SortBy` 문자열을 `switch expression`으로 분기한다.  
`_` 케이스가 default 역할을 하며 `CreatedAt` 기준 정렬로 fallback된다.

**대댓글 수 별도 집계**

```csharp
var replyCounts = await _db.Comments
    .Where(c => c.ParentCommentId.HasValue && commentIds.Contains(c.ParentCommentId.Value))
    .GroupBy(c => c.ParentCommentId!.Value)
    .Select(g => new { CommentId = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.CommentId, x => x.Count);
```

목록 페이지에서 각 원댓글의 대댓글 수를 표시하기 위해  
**별도 쿼리로 집계 후 Dictionary로 변환**한다.  
이후 `replyCounts.GetValueOrDefault(c.Id, 0)`으로 O(1) 조회한다.

**GetCommentDetailAsync — IsReply() 분기**

```csharp
public async Task<AdminCommentResponse> GetCommentDetailAsync(int commentId)
{
    var comment = await _db.Comments.FindAsync(commentId);

    if (comment.IsReply())
        return MapToAdminCommentResponse(comment);  // 대댓글: replies 없이 반환

    var replies = await _db.Comments
        .Where(c => c.ParentCommentId == commentId)
        .OrderBy(c => c.CreatedAt)
        .ToListAsync();

    return MapToAdminCommentResponse(comment, replies);  // 원댓글: replies 포함
}
```

도메인의 `IsReply()` 메서드로 분기해서  
원댓글이면 하위 대댓글을 포함해 반환하고, 대댓글이면 그대로 반환한다.

---

### 3.2 PublicCommentService

**CreateCommentAsync — 게시글 상태 선검증**

```csharp
public async Task<CommentResponse> CreateCommentAsync(long postId, long authorId, CreateCommentRequest request)
{
    var post = await _db.Posts.FindAsync(postId);
    if (post == null)       throw new CommentNotFoundException(postId);
    if (post.IsDeleted)     throw new InvalidPostStateException("삭제된 게시글에는 댓글을 작성할 수 없습니다");

    var comment = new Comment(postId, authorId, request.Comment);
    _db.Comments.Add(comment);
    await _db.SaveChangesAsync();
    ...
}
```

댓글을 달기 전에 **게시글 존재 여부와 삭제 여부를 먼저 확인**한다.

**CreateReplyAsync — 대댓글 3단계 검증**

```csharp
public async Task<ReplyResponse> CreateReplyAsync(long postId, long parentCommentId, ...)
{
    // 1. 게시글 유효성
    var post = await _db.Posts.FindAsync(postId);
    if (post == null || post.IsDeleted) throw ...;

    // 2. 부모 댓글 존재 여부
    var parentComment = await _db.Comments.FindAsync(parentCommentId);
    if (parentComment == null) throw new ParentCommentNotFoundException(parentCommentId);

    // 3. 대댓글의 대댓글 방지 (1단계 제한)
    if (parentComment.PostId != postId)
        throw InvalidParentCommentException.NestedReplyNotAllowed(parentCommentId);

    // 4. 부모 댓글 삭제 여부
    if (parentComment.Status == CommentStatus.Deleted)
        throw InvalidParentCommentException.ParentDeleted(parentCommentId);
    ...
}
```

대댓글 생성 시 검증 단계가 4단계다.  
특히 `parentComment.PostId != postId` 체크로 **다른 게시글의 댓글에 대댓글 다는 것**을 막는다.

**GetCommentsByPostAsync — 작성자 이름 일괄 조회**

```csharp
// 1. 해당 게시글의 Active 댓글 전체 로드
var allComments = await _db.Comments
    .Where(c => c.PostId == postId && c.Status == CommentStatus.Active)
    .OrderBy(c => c.CreatedAt)
    .ToListAsync();

// 2. 작성자 ID 목록으로 닉네임 한 번에 조회
var authorIds   = allComments.Select(c => c.AuthorId).Distinct().ToList();
var authorNames = await _db.Users
    .Where(u => authorIds.Contains(u.UserId))
    .ToDictionaryAsync(u => u.UserId, u => u.Nickname);

// 3. 메모리에서 원댓글/대댓글 분리 후 조립
var rootComments = allComments.Where(c => !c.ParentCommentId.HasValue).ToList();
var commentResponses = rootComments.Select(root =>
{
    var replies = allComments.Where(c => c.ParentCommentId == root.Id).ToList();
    return MapToCommentResponse(root, replies, authorNames);
}).ToList();
```

**Post.AuthorName과 설계 방식이 다르다.**

| 구분 | Post | Comment |
|------|------|---------|
| 작성자 이름 저장 방식 | DB에 직접 저장 (비정규화) | Users 테이블에서 조회 |
| 조회 시점 | 저장 시 1회만 | 댓글 목록 조회 때마다 |
| N+1 방지 방법 | 해당 없음 | authorIds로 한 번에 조회 후 Dictionary |

댓글은 수가 많을 수 있어 작성자마다 쿼리를 날리는 N+1 문제가 생기기 쉽다.  
이를 `Dictionary<long, string>`으로 한 번에 로드해서 해결한다.

---

### 3.3 두 서비스 비교

| 항목 | AdminCommentService | PublicCommentService |
|------|--------------------|--------------------|
| 인증 주체 | Controller `[Authorize]`에 위임 | `authorId` 파라미터로 직접 받음 |
| 소유권 검증 | 없음 (Admin은 모든 댓글 접근) | `comment.IsAuthor(authorId)` |
| 삭제 방식 | SoftDelete + HardDelete | SoftDelete만 |
| 복구 | `RestoreCommentAsync()` 있음 | 없음 |
| 작성자 이름 | DTO에 미포함 | `authorNames` Dictionary로 조합 |
| 대댓글 수 | `replyCounts` 별도 집계 | `ReplyCount` 필드로 표현 |

---

## 4. Controller 계층

### 4.1 AdminCommentController

```csharp
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/comments")]
public class AdminCommentController : ControllerBase
```

**엔드포인트 목록**

| Method | URL | 동작 |
|--------|-----|------|
| GET | `/api/admin/comments` | 목록 조회 (필터/정렬/페이징) |
| GET | `/api/admin/comments/{commentId}` | 상세 조회 (대댓글 포함) |
| DELETE | `/api/admin/comments/{commentId}` | 소프트 딜리트 |
| POST | `/api/admin/comments/{commentId}/restore` | 복구 |
| DELETE | `/api/admin/comments/{commentId}/permanent` | 하드 딜리트 |

Post의 하드 딜리트 URL이 `/hard`인 반면, Comment는 `/permanent`다.  
두 도메인이 같은 기능에 다른 URL 컨벤션을 사용하고 있다.

---

### 4.2 BoardCommentController

```csharp
[Authorize]  // 클래스 전체 — 비로그인 접근 불가
public sealed class BoardCommentController : Controller
```

`BoardController`와 달리 **클래스 전체에 `[Authorize]`** 가 붙어있다.  
댓글 작성·삭제·대댓글 모두 로그인 필수다.

**엔드포인트 목록**

| Method | URL | 동작 |
|--------|-----|------|
| POST | `/board/{postId}/comments` | 댓글 작성 |
| POST | `/board/{postId}/comments/{commentId}/delete` | 댓글 삭제 |
| POST | `/board/{postId}/comments/{parentCommentId}/replies` | 대댓글 작성 |

모든 엔드포인트가 `POST`다. HTML Form은 GET/POST만 지원하기 때문에  
삭제도 DELETE가 아닌 `/delete` suffix를 붙인 POST로 처리한다.

**`[ValidateAntiForgeryToken]` 적용**

```csharp
[HttpPost("/board/{postId:long}/comments")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateComment(long postId, [FromForm] string comment)
{
    await _commentService.CreateCommentAsync(postId, _userContext.ActorId, ...);
    return RedirectToAction("Detail", "Board", new { id = postId });
}
```

Razor Form에서 전송되는 요청이므로 CSRF 방어를 위해 `[ValidateAntiForgeryToken]`을 붙인다.  
처리 후 `RedirectToAction`으로 게시글 상세 페이지로 돌아간다.

---

## 5. Post와의 차이점

| 항목 | Post | Comment |
|------|------|---------|
| 계층 구조 | 없음 (단일) | 원댓글 → 대댓글 (1단계) |
| 작성자 이름 | DB에 비정규화 저장 | Users 테이블에서 조회 |
| 예외 생성 패턴 | `new Exception("메시지")` | `Exception.FactoryMethod()` |
| 유효성 예외 타입 | `DomainException` | `ArgumentException` |
| 조회수 | `IncrementViewCount()` 있음 | 없음 |
| Public 복구 | 불가 | 불가 |
| Admin 복구 | `RestoreAsync()` 있음 | `RestoreCommentAsync()` 있음 |

---

## 6. 미구현 항목 및 버그

### 미구현 (NotImplementedException)

현재 두 곳이 미구현 상태다.

```csharp
// AdminCommentService
public Task DeleteCommentAsync(int commentId)
{
    throw new NotImplementedException();  // ← 미구현
}

// PublicCommentService
public Task<PagedResponse<AdminCommentListItemDto>> GetAllCommentsAsync(AdminCommentListQuery query)
{
    throw new NotImplementedException();  // ← 미구현
}
```

`AdminCommentController.DeleteComment()`는 `DeleteCommentAsync()`를 호출하므로  
현재 Admin 댓글 삭제 API를 호출하면 500 에러가 발생한다.

---

### HardDeleteCommentAsync 버그

```csharp
public async Task HardDeleteCommentAsync(int commentId)
{
    ...
    if (comment.IsReply() == false)
    {
        var replies = await _db.Comments
            .Where(c => c.ParentCommentId == commentId)
            .ToListAsync();

        _db.Comments.RemoveRange(comment);  // ← 버그: replies가 아닌 comment를 넘기고 있음
    }

    _db.Comments.Remove(comment);
    await _db.SaveChangesAsync();
}
```

**의도한 동작:** 원댓글 삭제 시 하위 대댓글도 함께 삭제  
**실제 동작:** `RemoveRange(comment)`에 단일 객체를 넘겨서 대댓글이 삭제되지 않음

**올바른 코드:**
```csharp
_db.Comments.RemoveRange(replies);  // replies로 수정 필요
```
