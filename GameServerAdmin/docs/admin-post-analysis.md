# Admin Post 기능 분석

> 대상 프로젝트: `GameServerAdmin`  
> 분석 범위: Domain → Service → Controller (Admin 전용)  
> 작성일: 2026-04-21

---

## 목차

1. [전체 구조 한눈에 보기](#1-전체-구조-한눈에-보기)
2. [Admin 진입 — 권한 정책](#2-admin-진입--권한-정책)
3. [IAdminPostService 인터페이스](#3-iadminpostservice-인터페이스)
4. [AdminPostService 구현](#4-adminpostservice-구현)
   - [목록 조회](#41-목록-조회--getadminpostlistasync)
   - [상세 조회](#42-상세-조회--getpostdetailforadminasync)
   - [수정](#43-수정--updateasync)
   - [소프트 딜리트](#44-소프트-딜리트--softdeleteasync)
   - [복구](#45-복구--restoreasync)
   - [하드 딜리트](#46-하드-딜리트--harddeleteasync)
5. [AdminPostController — REST API](#5-adminpostcontroller--rest-api)
6. [AdminUiPostController — Razor UI](#6-adminuipostcontroller--razor-ui)
7. [두 Controller 비교](#7-두-controller-비교)
8. [삭제 정책 — Soft → Hard 순서 강제](#8-삭제-정책--soft--hard-순서-강제)
9. [Public과의 차이점 정리](#9-public과의-차이점-정리)

---

## 1. 전체 구조 한눈에 보기

```
[Admin Tool / Admin UI]
         │
         ▼
┌──────────────────────────────────────────────────────┐
│                  Controller 계층                      │
│  AdminPostController       AdminUiPostController      │
│  (REST API, JSON 반환)      (Razor View 반환)          │
│  /api/admin/posts          /admin/ui/posts            │
└──────────────────────────────────────────────────────┘
         │                           │
         └─────────────┬─────────────┘
                       ▼
┌──────────────────────────────────────────────────────┐
│                  Service 계층                         │
│                IAdminPostService                      │
│                AdminPostService                       │
└──────────────────────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────┐
│                  Domain 계층 (공통)                   │
│  Post  /  PostStatus                                 │
└──────────────────────────────────────────────────────┘
```

**포인트:** Admin은 두 종류의 Controller를 가진다.

| Controller | 목적 | 반환 |
|---|---|---|
| `AdminPostController` | 외부 API 연동, 운영 툴 | JSON |
| `AdminUiPostController` | 브라우저 관리 페이지 | Razor View |

둘 다 동일한 `IAdminPostService`를 사용한다.

---

## 2. Admin 진입 — 권한 정책

```csharp
// AdminPostController
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/posts")]
public sealed class AdminPostController : ControllerBase

// AdminUiPostController
[Authorize(Policy = "AdminOnly")]
public class AdminUiPostController : Controller
```

두 Controller 모두 클래스 레벨에 `[Authorize(Policy = "AdminOnly")]`가 붙어있다.

**Public과의 차이**

| 구분 | Public | Admin |
|------|--------|-------|
| 인증 방식 | `[Authorize]` (JWT 있으면 OK) | `[Authorize(Policy = "AdminOnly")]` (역할 필요) |
| 권한 실패 | 401 Unauthorized | 403 Forbidden |
| 소유권 검증 | Service 내부 `EnsureOwner()` | 없음 (Admin은 모든 글 접근) |

---

## 3. IAdminPostService 인터페이스

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

**`CreateAsync`가 없다**

Admin 전용 게시글 생성 메서드가 인터페이스에 없다.  
현재 Admin이 게시글을 작성하려면 일반 유저와 동일한 `/board/create` 경로를 사용한다.  
`Models/Posts/AdminApi/PostCreateRequest.cs` 파일이 존재하지만 현재 미사용 상태다.  
→ Admin 전용 생성 경로는 향후 구현 예정으로 보인다.

---

## 4. AdminPostService 구현

### 4.1 목록 조회 — GetAdminPostListAsync

```csharp
private const int MaxPageSize = 200;

public async Task<PagedResponse<AdminPostListItemResponse>> GetAdminPostListAsync(AdminPostListQuery query)
{
    // 쿼리 유효성 검사
    var errors = new FieldErrorCollection();
    if (query.Page <= 0)      errors.AddError(...);
    if (query.PageSize <= 0)  errors.AddError(...);
    if (query.PageSize > MaxPageSize) errors.AddError(...);
    if (errors.Any()) throw new RequestValidationException(errors);

    var postsQuery = _db.Posts.AsNoTracking().AsQueryable();

    if (query.IsDeleted.HasValue)
        postsQuery = postsQuery.Where(p => p.IsDeleted == query.IsDeleted.Value);

    var totalCount = await postsQuery.CountAsync();

    var items = await postsQuery
        .OrderByDescending(p => p.PostId)
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .Select(p => new AdminPostListItemResponse { ... })
        .ToListAsync();

    return new PagedResponse<AdminPostListItemResponse> { TotalCount = totalCount, Items = items };
}
```

**포인트 2가지**

1. **`MaxPageSize = 200` 상한**  
   페이지당 최대 200개로 제한한다. 운영 툴에서 과도한 데이터 조회를 방지한다.

2. **`IsDeleted` 필터**  
   `query.IsDeleted`가 `null`이면 전체(삭제된 것 포함), `true`면 삭제된 것만, `false`면 활성 글만 조회한다.  
   Public에서는 `Where(p => !p.IsDeleted)`가 고정이지만, Admin은 삭제된 글도 볼 수 있다.

---

### 4.2 상세 조회 — GetPostDetailForAdminAsync

```csharp
public async Task<AdminPostDetailResponse> GetPostDetailForAdminAsync(int postId)
{
    var post = await _db.Posts
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.PostId == postId);  // IsDeleted 필터 없음

    if (post is null)
        throw new PostNotFoundException(postId);

    return new AdminPostDetailResponse
    {
        ...
        DeletedAt = post.DeletedAt  // 삭제 시각 포함
    };
}
```

Public의 `GetByIdAsync`와 두 가지 차이가 있다.

| 항목 | Public | Admin |
|------|--------|-------|
| 삭제된 글 조회 | 불가 (`!p.IsDeleted` 필터) | 가능 (필터 없음) |
| 조회수 증가 | `IncrementViewCount()` 호출 | 없음 |
| 반환 필드 | `ViewCount` 포함 | `DeletedAt` 포함 |

---

### 4.3 수정 — UpdateAsync

```csharp
public async Task UpdateAsync(AdminPostUpdateRequest request)
{
    // 1. App Validation
    var errors = new FieldErrorCollection();
    if (request.PostId <= 0) errors.AddError(...);
    if (string.IsNullOrWhiteSpace(request.Title)) errors.AddError(...);
    if (string.IsNullOrWhiteSpace(request.Content)) errors.AddError(...);
    if (errors.Any()) throw new RequestValidationException(errors);

    // 2. Post 조회
    var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == request.PostId);
    if (post is null) throw new PostNotFoundException(request.PostId);

    // 3. 삭제된 글 수정 방지
    if (post.IsDeleted)
        throw new InvalidPostStateException("삭제된 Post는 수정할 수 없습니다.");

    // 4. Domain 메서드 호출 (내부에서 유효성 재검사)
    post.Update(request.Title, request.Content);

    await _db.SaveChangesAsync();
}
```

**유효성 검사가 두 단계다**

```
Service: App Validation (빈값, postId > 0)
              ↓
Domain:  post.Update() 내부 ValidateTitle / ValidateContent (길이 등)
```

Service가 먼저 기본 검증을 하고, Domain이 비즈니스 규칙 검증을 한다.

---

### 4.4 소프트 딜리트 — SoftDeleteAsync

```csharp
public async Task SoftDeleteAsync(int postId)
{
    var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
    if (post is null)    throw new PostNotFoundException(postId);
    if (post.IsDeleted)  throw new InvalidPostStateException("이미 삭제된 Post입니다.");

    post.SoftDelete();  // Status = Deleted, IsDeleted = true, DeletedAt = now
    await _db.SaveChangesAsync();
}
```

실제 DB에서 행을 제거하지 않는다. `IsDeleted = true`로 표시만 한다.  
목록 조회 시 Admin은 여전히 볼 수 있고, Public에서는 필터링되어 안 보인다.

---

### 4.5 복구 — RestoreAsync

```csharp
public async Task RestoreAsync(int postId)
{
    var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
    if (post is null)    throw new PostNotFoundException(postId);
    if (!post.IsDeleted) throw new InvalidPostStateException("Post is not deleted.");

    post.Restore();  // Status = Active, IsDeleted = false, DeletedAt = null
    await _db.SaveChangesAsync();
}
```

`SoftDelete`의 역방향이다. `DeletedAt`이 `null`로 초기화된다.  
Public에는 이 기능이 없다. Admin 전용이다.

---

### 4.6 하드 딜리트 — HardDeleteAsync

```csharp
public async Task HardDeleteAsync(int postId)
{
    var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
    if (post is null)    throw new PostNotFoundException(postId);

    if (!post.IsDeleted)
        throw new InvalidPostStateException("Post must be soft-deleted before hard delete.");

    _db.Posts.Remove(post);
    await _db.SaveChangesAsync();
}
```

**SoftDelete 상태가 아니면 HardDelete 불가.** 순서를 강제한다.  
`_db.Posts.Remove()`로 DB에서 행이 실제로 제거된다. 복구 불가능하다.

---

## 5. AdminPostController — REST API

```csharp
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/posts")]
public sealed class AdminPostController : ControllerBase
```

**엔드포인트 목록**

| Method | URL | 동작 | 반환 |
|--------|-----|------|------|
| GET | `/api/admin/posts` | 목록 조회 (페이징) | 200 + JSON |
| GET | `/api/admin/posts/{id}` | 상세 조회 | 200 + JSON |
| PUT | `/api/admin/posts/{id}` | 수정 | 204 No Content |
| DELETE | `/api/admin/posts/{id}` | 소프트 딜리트 | 204 No Content |
| PATCH | `/api/admin/posts/{id}/restore` | 복구 | 204 No Content |
| DELETE | `/api/admin/posts/{id}/hard` | 하드 딜리트 | 204 No Content |

**수정 시 Route id와 Body id 불일치 방어**

```csharp
[HttpPut("{id:int}")]
public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AdminPostUpdateRequest request)
{
    if (request.PostId != id)
    {
        var errors = new FieldErrorCollection();
        errors.AddError(nameof(request.PostId), "Body PostId must match route id.");
        throw new RequestValidationException(errors);
    }
    await _postService.UpdateAsync(request);
    return NoContent();
}
```

URL의 `{id}`와 Body의 `PostId`가 다를 경우 즉시 거부한다.  
클라이언트 실수 또는 의도적인 조작을 방어하는 코드다.

---

## 6. AdminUiPostController — Razor UI

```csharp
[Authorize(Policy = "AdminOnly")]
public class AdminUiPostController : Controller  // Controller (View 지원)
```

**엔드포인트 목록**

| Method | URL | 동작 |
|--------|-----|------|
| GET | `/admin/ui/posts` | 목록 페이지 |
| GET | `/admin/ui/posts/{id}` | 상세 페이지 |
| GET | `/admin/ui/posts/{id}/edit` | 수정 폼 |
| POST | `/admin/ui/posts/{id}/edit` | 수정 처리 |
| POST | `/admin/ui/posts/{id}/soft-delete` | 소프트 딜리트 |
| POST | `/admin/ui/posts/{id}/restore` | 복구 |
| POST | `/admin/ui/posts/{id}/hard-delete` | 하드 딜리트 |

**수정 흐름 — id 불일치 방어**

```csharp
[HttpPost("admin/ui/posts/{id}/edit")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, AdminPostEditViewModel model)
{
    if (id != model.PostId)
        return BadRequest();           // REST API와 동일한 방어 로직
    if (!ModelState.IsValid)
        return View("~/Views/AdminPost/Edit.cshtml", model);  // 폼 재표시

    await _postService.UpdateAsync(request);
    return RedirectToAction(nameof(Detail), new { id = model.PostId });
}
```

REST API Controller와 동일하게 `id != model.PostId` 방어가 있다.

**삭제 관련 엔드포인트 — 모두 POST**

```csharp
[HttpPost("/admin/ui/posts/{id}/soft-delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SoftDelete(int id) { ... return RedirectToAction(nameof(Index)); }

[HttpPost("/admin/ui/posts/{id}/hard-delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> HardDelete(int id) { ... return RedirectToAction(nameof(Index)); }
```

HTML Form은 GET/POST만 지원하므로 삭제도 POST로 처리한다.  
처리 후에는 목록 페이지로 Redirect한다.

---

## 7. 두 Controller 비교

| 항목 | AdminPostController | AdminUiPostController |
|------|--------------------|-----------------------|
| 상속 | `ControllerBase` | `Controller` |
| 응답 형식 | JSON | Razor View |
| 수정 Method | `PUT` | `POST` |
| 삭제 Method | `DELETE` | `POST` |
| 복구 Method | `PATCH` | `POST` |
| CSRF 방어 | `[ApiController]` 자동 처리 | `[ValidateAntiForgeryToken]` 명시 |
| 처리 후 응답 | `NoContent()` (204) | `RedirectToAction()` |
| 하드 딜리트 URL suffix | `/hard` | `/hard-delete` |

하드 딜리트 URL suffix가 두 Controller 간에 다르다(`/hard` vs `/hard-delete`).  
기능은 동일하지만 URL 컨벤션이 통일되어 있지 않다.

---

## 8. 삭제 정책 — Soft → Hard 순서 강제

```
[Active 상태]
     │
     ├─ SoftDeleteAsync() ──→ [Deleted 상태]
     │                               │
     │   ◀── RestoreAsync() ─────────┘
     │                               │
     │                        HardDeleteAsync() ──→ [DB에서 영구 제거]
     │
     └─ HardDeleteAsync() ──→ InvalidPostStateException ❌
                               "Must be soft-deleted before hard delete"
```

**왜 이 정책이 필요한가**

실수로 즉시 영구 삭제되는 상황을 방지한다.  
SoftDelete → 확인 → HardDelete의 2단계를 거쳐야 하므로  
삭제 전에 복구 기회가 반드시 한 번 생긴다.

---

## 9. Public과의 차이점 정리

| 항목 | PublicPostService | AdminPostService |
|------|-----------------|-----------------|
| 게시글 생성 | `CreateAsync()` 있음 | **없음** (미구현) |
| 소유권 검증 | `EnsureOwner()` 필수 | 없음 |
| 삭제된 글 조회 | 불가 | 가능 |
| 조회수 증가 | 상세 조회 시 자동 | 없음 |
| 복구 | 불가 | `RestoreAsync()` |
| 영구 삭제 | 불가 | `HardDeleteAsync()` |
| 반환 DTO | `PublicPostDetailResponse` | `AdminPostDetailResponse` (DeletedAt 포함) |
