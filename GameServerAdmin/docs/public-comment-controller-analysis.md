# PublicCommentController 분석

> 대상 프로젝트: `GameServerAdmin`  
> 분석 범위: PublicCommentController (버그 수정 후)  
> 작성일: 2026-04-21

---

## 목차

1. [클래스 선언부](#1-클래스-선언부)
2. [엔드포인트 목록](#2-엔드포인트-목록)
3. [각 엔드포인트 분석](#3-각-엔드포인트-분석)
   - [CreateComment](#31-createcomment--댓글-작성)
   - [CreateReply](#32-createreply--대댓글-작성)
   - [UpdateComment](#33-updatecomment--댓글-수정)
   - [DeleteComment](#34-deletecomment--댓글-삭제)
   - [GetComments](#35-getcomments--댓글-목록-조회)
4. [인증 적용 방식](#4-인증-적용-방식)
5. [BoardCommentController와의 비교](#5-boardcommentcontroller와의-비교)
6. [수정 이력 — 버그 수정 내역](#6-수정-이력--버그-수정-내역)

---

## 1. 클래스 선언부

```csharp
[ApiController]
[Route("api/posts/{postId}/comments")]
public class PublicCommentController : ControllerBase
```

**포인트 3가지**

- `ControllerBase` 상속 → View 없음, 순수 REST API
- `[Route("api/posts/{postId}/comments")]` → 클래스 레벨 라우트. `postId`가 모든 엔드포인트에서 공유됨
- 클래스 레벨에 `[Authorize]` 없음 → 조회(`GetComments`)는 비인증 허용, 나머지는 메서드별로 개별 적용

---

## 2. 엔드포인트 목록

| Method | URL | 인증 | 설명 |
|--------|-----|------|------|
| POST | `/api/posts/{postId}/comments` | ✅ 필요 | 댓글 작성 |
| POST | `/api/posts/{postId}/comments/{commentId}/replies` | ✅ 필요 | 대댓글 작성 |
| PUT | `/api/posts/{postId}/comments/{commentId}` | ✅ 필요 | 댓글 수정 |
| DELETE | `/api/posts/{postId}/comments/{commentId}` | ✅ 필요 | 댓글 삭제 |
| GET | `/api/posts/{postId}/comments` | ❌ 불필요 | 댓글 목록 조회 |

---

## 3. 각 엔드포인트 분석

### 3.1 CreateComment — 댓글 작성

```csharp
[Authorize]
[HttpPost]
public async Task<ActionResult<CommentResponse>> CreateComment(
    long postId, [FromBody] CreateCommentRequest request)
{
    var authorId = _userContext.ActorId;

    var response = await _commentService.CreateCommentAsync(postId, authorId, request);
    return CreatedAtAction(nameof(GetComments), new { postId }, response);
}
```

**포인트**

- `authorId`를 `_userContext.ActorId`에서 가져온다. 현재 인증된 사용자의 ID만 사용 가능
- `CreatedAtAction` 반환 → HTTP 201 Created + `Location` 헤더에 댓글 목록 URL 포함

**Service에서 처리되는 검증**

```
① 게시글 존재 여부
② 게시글 삭제 여부 (IsDeleted)
③ 댓글 생성 → Domain 유효성 (빈값, 1000자 초과)
```

---

### 3.2 CreateReply — 대댓글 작성

```csharp
[Authorize]
[HttpPost("{commentId}/replies")]
public async Task<ActionResult<ReplyResponse>> CreateReply(
    [FromRoute] int postId, [FromRoute] int commentId,
    [FromBody] CreateReplyRequest request)
{
    var authorId = _userContext.ActorId;

    var response = await _commentService.CreateReplyAsync(postId, commentId, authorId, request);
    return CreatedAtAction(nameof(GetComments), new { postId }, response);
}
```

**포인트**

- `{commentId}`가 부모 댓글 ID — URL 구조상 "이 댓글의 replies" 의미
- `authorId`를 `_userContext.ActorId`에서 가져온다

**Service에서 처리되는 4단계 검증**

```
① 게시글 존재 및 삭제 여부
② 부모 댓글 존재 여부
③ parentComment.PostId == postId (다른 게시글 댓글에 대댓글 방지)
④ 부모 댓글 삭제 여부
```

---

### 3.3 UpdateComment — 댓글 수정

```csharp
[Authorize]
[HttpPut("{commentId}")]
public async Task<ActionResult<CommentResponse>> UpdateComment(
    [FromRoute] int postId, [FromRoute] int commentId,
    [FromBody] UpdateCommentRequest request)
{
    var authorId = _userContext.ActorId;
    var response = await _commentService.UpdateCommentAsync(postId, commentId, authorId, request);
    return Ok(response);
}
```

**포인트**

- `authorId`를 `_userContext.ActorId`에서 가져온다
- Service 내부에서 `comment.IsAuthor(authorId)` 검증 → 본인 댓글만 수정 가능
- 원댓글 수정 시 하위 대댓글도 함께 응답에 포함됨 (Service 처리)

---

### 3.4 DeleteComment — 댓글 삭제

```csharp
[Authorize]
[HttpDelete("{commentId}")]
public async Task<IActionResult> DeleteComment(
    [FromRoute] long postId, [FromRoute] long commentId)
{
    var authorId = _userContext.ActorId;
    await _commentService.DeleteCommentAsync(postId, commentId, authorId, _userContext.ActorType);
    return NoContent();
}
```

**포인트**

- `authorId`와 `ActorType` 둘 다 `_userContext`에서 가져온다
- `ActorType`을 함께 넘기는 이유 — Service에서 `AuthorId == 0`(작성자 불명) 케이스를 Admin만 삭제할 수 있도록 처리하기 위해
- 204 No Content 반환 → 삭제 성공 시 본문 없음

**Service 소유권 검증 흐름**

```
comment.IsAuthor(authorId)?
  → false: ForbiddenException ("본인이 작성한 댓글만 삭제할 수 있습니다.")
  → true:  comment.SoftDelete()
```

---

### 3.5 GetComments — 댓글 목록 조회

```csharp
[HttpGet]
public async Task<ActionResult<CommentListResponse>> GetComments([FromRoute] int postId)
{
    var response = await _commentService.GetCommentsByPostAsync(postId);
    return Ok(response);
}
```

**포인트**

- `[Authorize]` 없음 → 비로그인 상태에서도 조회 가능
- `CommentListResponse`에는 원댓글과 대댓글이 계층적으로 조합되어 반환됨

**Service에서 반환하는 구조**

```
CommentListResponse
├─ PostId
├─ TotalCount   (원댓글 + 대댓글 합계)
├─ CommentCount (원댓글만)
├─ ReplyCount   (대댓글만)
└─ Comments[]
    ├─ CommentResponse (원댓글)
    │   ├─ AuthorName  (Users 테이블에서 조회)
    │   └─ Replies[]
    │       └─ ReplyResponse (대댓글)
    └─ ...
```

작성자 이름은 댓글 목록 조회 시 Users 테이블에서 일괄 조회 후 Dictionary로 매핑된다.  
N+1 문제 없이 단 2번의 쿼리로 처리된다.

---

## 4. 인증 적용 방식

```
클래스 [Authorize] 없음
      │
      ├─ CreateComment  [Authorize] ✅  → _userContext.ActorId 사용
      ├─ CreateReply    [Authorize] ✅  → _userContext.ActorId 사용
      ├─ UpdateComment  [Authorize] ✅  → _userContext.ActorId 사용
      ├─ DeleteComment  [Authorize] ✅  → _userContext.ActorId 사용
      └─ GetComments    없음        ❌  → 누구나 조회 가능
```

쓰기 작업(작성/수정/삭제)은 모두 인증 필수,  
읽기 작업(조회)만 비인증 허용으로 일관성이 있다.

---

## 5. BoardCommentController와의 비교

같은 `IPublicCommentService`를 사용하지만 진입 경로와 용도가 다르다.

| 항목 | PublicCommentController | BoardCommentController |
|------|------------------------|----------------------|
| 상속 | `ControllerBase` | `Controller` |
| 응답 형식 | JSON | Redirect (Razor) |
| 인증 위치 | 메서드별 `[Authorize]` | 클래스 전체 `[Authorize]` |
| CSRF 방어 | `[ApiController]` 자동 | `[ValidateAntiForgeryToken]` 명시 |
| 삭제 Method | `DELETE` | `POST` (`/delete` suffix) |
| 용도 | 외부 API 클라이언트 | 브라우저 게시판 UI |

---

## 6. 수정 이력 — 버그 수정 내역

| 메서드 | 수정 전 | 수정 후 |
|--------|--------|--------|
| `CreateComment` | `NameIdentifier` 직접 파싱 (AppUser.Id) | `_userContext.ActorId` 사용 |
| `CreateReply` | `[Authorize]` 없음, `[FromQuery] authorId` | `[Authorize]` 추가, `_userContext.ActorId` 사용 |
| `UpdateComment` | `[Authorize]` 없음, `[FromQuery] authorId` | `[Authorize]` 추가, `_userContext.ActorId` 사용 |
| `DeleteComment` | `[FromQuery] authorId` (위장 가능) | `_userContext.ActorId` 사용 |

**수정의 핵심 — authorId 출처 변경**

```
수정 전: 클라이언트가 Query로 authorId 지정 → 타인 ID로 위장 가능
수정 후: _userContext.ActorId → 실제 인증된 사용자 ID만 사용
```
