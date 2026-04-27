# 게시판 Post 기능 분석

> 대상 프로젝트: `GameServerAdmin`  
> 분석 범위: Domain → Service → Controller  
> 작성일: 2026-04-21

---

## 목차

1. [전체 구조 한눈에 보기](#1-전체-구조-한눈에-보기)
2. [Domain 계층](#2-domain-계층)
   - [PostStatus](#21-poststatus)
   - [Post](#22-post)
3. [Service 계층](#3-service-계층)
   - [AdminPostService](#31-adminpostservice)
   - [PublicPostService](#32-publicpostservice)
   - [두 서비스 비교](#33-두-서비스-비교)
4. [Controller 계층](#4-controller-계층)
   - [AdminPostController](#41-adminpostcontroller)
   - [BoardController](#42-boardcontroller)
5. [설계 포인트 정리](#5-설계-포인트-정리)

---

## 1. 전체 구조 한눈에 보기

```
[Client / Admin Tool]
        │
        ▼
┌──────────────────────────────────────────────┐
│              Controller 계층                  │
│  BoardController        AdminPostController   │
│  (Razor View 반환)       (JSON 반환)           │
└──────────────────────────────────────────────┘
        │                        │
        ▼                        ▼
┌──────────────────────────────────────────────┐
│              Service 계층                     │
│  PublicPostService      AdminPostService      │
│  (소유권 검증 포함)       (관리자 전용 기능)    │
└──────────────────────────────────────────────┘
        │                        │
        └──────────┬─────────────┘
                   ▼
┌──────────────────────────────────────────────┐
│              Domain 계층 (공통)               │
│  Post  /  PostStatus                         │
└──────────────────────────────────────────────┘
```

**계층별 역할 요약**

| 계층 | 역할 |
|------|------|
| Domain | 비즈니스 규칙 보유. DB·HTTP를 모름 |
| Service | 유스케이스 구현. Domain 호출 + DB I/O 조율 |
| Controller | HTTP 요청 수신 → Service 호출 → 응답 반환 |

---

## 2. Domain 계층

### 2.1 PostStatus

```csharp
public enum PostStatus
{
    Active  = 0,
    Deleted = 1
}
```

**포인트**

- 값을 `= 0`부터 **명시적으로 고정**한다.  
  → DB에 integer로 저장되므로, 나중에 enum 순서가 바뀌어도 기존 DB 데이터가 깨지지 않는다.
- 상태가 **Active / Deleted** 2개뿐이다.  
  → `Hidden`, `Pending` 같은 중간 상태가 없는 단순한 게시판 설계에 적합하다.

---

### 2.2 Post

#### 클래스 선언 — 인터페이스 3개 구현

```csharp
public class Post : ICommentable, IViewCountable, ISoftDeletable
```

| 인터페이스 | 의미 |
|---|---|
| `ICommentable` | 댓글을 달 수 있는 대상임을 보장 |
| `IViewCountable` | 조회수를 증가시킬 수 있는 대상임을 보장 |
| `ISoftDeletable` | 논리 삭제 패턴을 따름 |

나중에 `Notice` 같은 다른 엔티티에 동일한 인터페이스를 붙이면,  
`Post`인지 `Notice`인지 몰라도 `ICommentable`로 댓글 처리가 가능하다.

---

#### 생성자

```csharp
public Post(string postType, string title, string content,
            ActorType authorType, long authorId, string authorName)
{
    ValidateTitle(title);
    ValidateContent(content);
    ...
    Status           = PostStatus.Active;
    ViewCount        = 0;
    IsCommentEnabled = true;
}
```

**설계 포인트 2가지**

1. **AuthorName 비정규화**  
   작성자 이름을 Post 테이블에 직접 저장한다.  
   → 유저가 닉네임을 바꾸거나 탈퇴해도 게시글에 원래 이름이 남는다.  
   → 목록 조회 시 User 테이블 JOIN이 필요 없다.  
   → 단점: 닉네임 변경이 과거 게시글에 반영되지 않는다. (의도된 트레이드오프)

2. **PostType이 string**  
   enum 대신 `"PUBLIC"`, `"NOTICE"` 등 문자열로 게시판 종류를 구분한다.  
   → enum이면 새 게시판 추가 시 코드 수정 + DB 마이그레이션이 필요하다.  
   → string이면 DB에 새 값을 넣는 것만으로 확장 가능하다.

---

#### 상태 변경 메서드

**Update()**

```csharp
public void Update(string title, string content)
{
    if (Status == PostStatus.Deleted)
        throw new InvalidPostStateException("삭제된 게시글은 수정할 수 없습니다.");

    ValidateTitle(title);
    ValidateContent(content);
    Title     = title;
    Content   = content;
    UpdatedAt = DateTime.UtcNow;
}
```

삭제된 글은 수정 불가. 상태 검증을 **도메인 내부**에서 처리한다.  
Service가 별도로 `if (post.IsDeleted)` 체크를 하지 않아도 된다.

---

**SoftDelete() / Restore()**

```csharp
public void SoftDelete()
{
    if (Status == PostStatus.Deleted)
        throw new InvalidPostStateException("이미 삭제된 게시글입니다.");

    Status    = PostStatus.Deleted;
    IsDeleted = true;
    DeletedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}

public void Restore()
{
    if (Status != PostStatus.Deleted)
        throw new InvalidPostStateException("삭제되지 않은 게시글은 복구할 수 없습니다.");

    Status    = PostStatus.Active;
    IsDeleted = false;
    DeletedAt = null;
    UpdatedAt = DateTime.UtcNow;
}
```

두 메서드가 **서로 역방향**이다.  
`Restore()`는 AdminPostService에서만 호출되고, Public에서는 접근이 없다.

> `IsDeleted(bool)`와 `Status(enum)` 두 필드가 동시에 관리된다.  
> `IsDeleted`는 EF Core의 `Where(p => !p.IsDeleted)` 필터링에,  
> `Status`는 도메인 규칙 검증에 각각 사용된다.

---

**IncrementViewCount()**

```csharp
public void IncrementViewCount() => ViewCount++;
```

`PublicPostService.GetByIdAsync()`에서 상세 조회할 때마다 호출된다.  
조회 + 카운트 증가가 **한 트랜잭션**으로 처리된다.

---

**EnableComments() / DisableComments()**

```csharp
public void EnableComments()  { IsCommentEnabled = true;  UpdatedAt = DateTime.UtcNow; }
public void DisableComments() { IsCommentEnabled = false; UpdatedAt = DateTime.UtcNow; }
```

댓글 허용 여부를 게시글 단위로 제어한다.  
현재 Admin 기능으로 제공되며, Public Service에서는 직접 호출하지 않는다.

---

#### 유효성 검사 — private 메서드 분리

```csharp
private static void ValidateTitle(string title)
{
    if (string.IsNullOrWhiteSpace(title))
        throw new DomainException("제목은 필수입니다.");
    if (title.Length > 200)
        throw new DomainException("제목은 200자를 초과할 수 없습니다.");
}

private static void ValidateContent(string content)
{
    if (string.IsNullOrWhiteSpace(content))
        throw new DomainException("내용은 필수입니다.");
}
```

생성자와 `Update()` 양쪽에서 동일한 검증 로직을 재사용한다.  
`private static`으로 선언해 인스턴스 상태와 무관하게 동작한다.

---

## 3. Service 계층

### 3.1 AdminPostService

**인터페이스**

```csharp
public interface IAdminPostService
{
    Task UpdateAsync(AdminPostUpdateRequest request);
    Task SoftDeleteAsync(int postId);
    Task RestoreAsync(int postId);
    Task HardDeleteAsync(int postId);
    Task<PagedResponse<AdminPostListItemResponse>> GetAdminPostListAsync(AdminPostListQuery query);
    Task<AdminPostDetailResponse> GetPostDetailForAdminAsync(int postId);
}
```

**특징**

- `IUserContext` 주입 없음 → 권한 검증은 Controller의 `[Authorize(Policy="AdminOnly")]`에서 처리
- `HardDelete`는 반드시 `SoftDelete` 이후에만 가능 (도메인 규칙 아닌 서비스 규칙)

```csharp
public async Task HardDeleteAsync(int postId)
{
    var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
    if (!post.IsDeleted)
        throw new InvalidPostStateException("Post must be soft-deleted before hard delete.");

    _db.Posts.Remove(post);
    await _db.SaveChangesAsync();
}
```

- 페이징 조회에 `MaxPageSize = 200` 상한 적용

```csharp
private const int MaxPageSize = 200;
```

---

### 3.2 PublicPostService

**인터페이스**

```csharp
public interface IPublicPostService
{
    Task<PublicPostDetailResponse> CreateAsync(PublicPostCreateRequest request);
    Task<List<PublicPostDetailResponse>> GetAllAsync();
    Task<PublicPostDetailResponse> GetByIdAsync(int postId);
    Task<PublicPostDetailResponse> UpdateAsync(int postId, PublicPostUpdateRequest request);
    Task DeleteAsync(int postId);
}
```

**소유권 검증 — 핵심**

```csharp
private void EnsureOwner(Post post)
{
    if (post.AuthorId != _userContext.ActorId || post.AuthorType != _userContext.ActorType)
        throw new ForbiddenException("본인이 작성한 게시글만 수정할 수 있습니다.");
}
```

`UpdateAsync()`에서 호출. 다른 유저의 글은 수정 불가.

**삭제 시 특수 케이스**

```csharp
private void EnsureCanDelete(Post post)
{
    bool isUnknownAuthor = post.AuthorId == 0;

    if (isUnknownAuthor)
    {
        if (_userContext.ActorType != ActorType.ADMIN)
            throw new ForbiddenException("작성자를 알 수 없는 게시글은 관리자만 삭제할 수 있습니다.");
        return;
    }

    if (post.AuthorId != _userContext.ActorId || post.AuthorType != _userContext.ActorType)
        throw new ForbiddenException("본인이 작성한 게시글만 삭제할 수 있습니다.");
}
```

`AuthorId == 0`은 작성자 불명 케이스. 이 경우 Admin만 삭제 가능하다.

**AuthorName 조회 흐름 (CreateAsync)**

```
ActorType == USER  → Users  테이블에서 Nickname 조회
ActorType == ADMIN → Admins 테이블에서 LoginId  조회
그 외             → "(알 수 없음)" fallback
```

Post를 저장하기 전에 작성자 이름을 직접 조회해서 `AuthorName`에 넣는다.

---

### 3.3 두 서비스 비교

| 항목 | AdminPostService | PublicPostService |
|------|-----------------|------------------|
| 인증 주체 | Controller `[Authorize]`에 위임 | `IUserContext` 직접 주입 |
| 소유권 검증 | 없음 (Admin은 모든 글 접근) | `EnsureOwner()` / `EnsureCanDelete()` |
| 삭제 방식 | SoftDelete + HardDelete | SoftDelete만 |
| 복구 | `RestoreAsync()` 있음 | 없음 |
| 조회수 증가 | 없음 | `GetByIdAsync()`에서 증가 |
| 반환 DTO | `AdminPostDetailResponse` | `PublicPostDetailResponse` |

---

## 4. Controller 계층

### 4.1 AdminPostController

```csharp
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/posts")]
public sealed class AdminPostController : ControllerBase
```

- `ControllerBase` → View 없음, 순수 REST API
- `[Authorize(Policy = "AdminOnly")]` → 클래스 전체에 적용

**엔드포인트 목록**

| Method | URL | 동작 |
|--------|-----|------|
| GET | `/api/admin/posts` | 목록 조회 (페이징) |
| GET | `/api/admin/posts/{id}` | 상세 조회 |
| PUT | `/api/admin/posts/{id}` | 수정 |
| DELETE | `/api/admin/posts/{id}` | 소프트 딜리트 |
| PATCH | `/api/admin/posts/{id}/restore` | 복구 |
| DELETE | `/api/admin/posts/{id}/hard` | 하드 딜리트 |

**Route id와 Body id 불일치 방어**

```csharp
public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AdminPostUpdateRequest request)
{
    if (request.PostId != id)
    {
        var errors = new FieldErrorCollection();
        errors.AddError(nameof(request.PostId), "Body PostId must match route id.");
        throw new RequestValidationException(errors);
    }
    ...
}
```

클라이언트 실수로 URL의 id와 Body의 PostId가 다를 경우를 방어한다.

---

### 4.2 BoardController

```csharp
public sealed class BoardController : Controller
```

- `Controller` 상속 → Razor View 반환 가능
- 글쓰기/삭제만 `[Authorize]`, 목록·상세는 비인증 허용

**엔드포인트 목록**

| Method | URL | 동작 |
|--------|-----|------|
| GET | `/board` | 게시글 목록 |
| GET | `/board/{id}` | 게시글 상세 + 댓글 |
| GET | `/board/create` | 작성 폼 |
| POST | `/board/create` | 게시글 등록 |
| POST | `/board/delete/{id}` | 게시글 삭제 |

**상세 조회 — Post와 Comment를 함께 로드**

```csharp
public async Task<IActionResult> Detail(int id)
{
    var post     = await _postService.GetByIdAsync(id);
    var comments = await _commentService.GetCommentsByPostAsync(id);

    var vm = new PublicBoardDetailViewModel
    {
        Post           = ...,
        Comments       = comments,
        IsAuthenticated = _userContext.IsAuthenticated,
        CurrentUserId   = _userContext.IsAuthenticated ? _userContext.ActorId : null
    };

    return View(vm);
}
```

`CurrentUserId`를 ViewModel에 넣는 이유 — View에서 "내가 쓴 댓글인지"를 판단해 수정/삭제 버튼 노출 여부를 결정하기 위해서다.

---

## 5. 설계 포인트 정리

### 1. 도메인이 규칙을 보유한다

Service가 `if (post.IsDeleted) throw ...`를 직접 쓰지 않는다.  
`post.Update()`, `post.SoftDelete()` 등 도메인 메서드 안에서 검증이 일어난다.

### 2. 삭제 정책이 Admin/Public에서 다르다

```
Public  : SoftDelete만 가능, 복구 불가
Admin   : SoftDelete → Restore → HardDelete (순서 강제)
```

HardDelete는 SoftDelete 상태인 게시글만 대상으로 한다.  
실수로 즉시 영구 삭제되는 것을 방지한다.

### 3. AuthorName 비정규화 트레이드오프

| 장점 | 단점 |
|------|------|
| 목록 조회 시 JOIN 불필요 | 닉네임 변경이 과거 글에 반영 안 됨 |
| 유저 탈퇴 후에도 이름 보존 | 데이터 일관성 관리 필요 |

### 4. PostType을 string으로 열어둔다

enum이 아닌 string으로 게시판 종류를 관리하면  
새 게시판 추가 시 코드 수정 없이 DB 데이터만으로 확장이 가능하다.

### 5. Controller는 얇다

비즈니스 로직이 Controller에 없다.  
파라미터 바인딩 → Service 호출 → 응답 반환, 이 세 가지만 한다.
