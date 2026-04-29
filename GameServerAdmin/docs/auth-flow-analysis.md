# 인증 / 인가 흐름 분석

> 대상 프로젝트: `GameServerAdmin`  
> 분석 범위: Program.cs 설정 → 로그인 → Claims → IUserContext → 권한 검사  
> 작성일: 2026-04-21

---

## 목차

1. [전체 흐름 한눈에 보기](#1-전체-흐름-한눈에-보기)
2. [AppUser — Identity 사용자 모델](#2-appuser--identity-사용자-모델)
3. [로그인 시 — Claims 생성](#3-로그인-시--claims-생성)
4. [요청 시 — SmartAuth 스킴 선택](#4-요청-시--smartauth-스킴-선택)
5. [IUserContext — Controller/Service에서 사용자 식별](#5-iusercontext--controllerservice에서-사용자-식별)
6. [ClaimsPrincipalExtensions — Claims 파싱](#6-claimsprincipalextensions--claims-파싱)
7. [권한 정책 — Authorize vs AdminOnly](#7-권한-정책--authorize-vs-adminonly)
8. [예외 처리 — ExceptionMiddleware](#8-예외-처리--exceptionmiddleware)
9. [테스트 인증 — TestAuthHandler](#9-테스트-인증--testauthhandler)
10. [전체 구성 요소 연결도](#10-전체-구성-요소-연결도)

---

## 1. 전체 흐름 한눈에 보기

```
[로그인]
  AppUser 조회 → AppUserClaimsPrincipalFactory
  → actor_id / actor_type 클레임 생성
  → Cookie 또는 JWT 토큰 발급
          │
          ▼
[요청 수신]
  SmartAuth 스킴 선택
  ├─ Authorization: Bearer ... 또는 /api/** → JWT 검증
  └─ 그 외 (UI)                            → Identity Cookie 검증
          │
          ▼
[Controller 진입 전]
  [Authorize]              → IsAuthenticated 확인
  [Authorize(AdminOnly)]   → Role = "Admin" or "SuperAdmin" 확인
          │
          ▼
[Controller / Service]
  IUserContext.ActorId    → ClaimsPrincipalExtensions.GetActorIdOrThrow()
  IUserContext.ActorType  → ClaimsPrincipalExtensions.GetActorTypeOrDefault()
          │
          ▼
[예외 발생 시]
  ExceptionMiddleware → HTTP 상태 코드 변환 후 JSON 응답
```

---

## 2. AppUser — Identity 사용자 모델

```csharp
public class AppUser : IdentityUser<long>
{
    public long?   AccountId { get; set; }   // 도메인 User.UserId와 연결
    public long?   AdminId   { get; set; }   // 도메인 Admin.AdminId와 연결
    public string  UserType  { get; set; } = "User";  // "User" | "Admin"
    public string? NickName  { get; set; }
    public DateTime  CreatedAt    { get; set; }
    public DateTime? LastLoginAt  { get; set; }
}
```

**설계 포인트 — 두 세계의 연결**

이 프로젝트는 Identity 사용자(`AppUser`)와 게임 도메인 사용자(`User`, `Admin`)가 분리되어 있다.

```
[Identity 세계]          [도메인 세계]
AppUser.Id (long)  ─→  AppUser.AccountId ─→  User.UserId
                   ─→  AppUser.AdminId   ─→  Admin.AdminId
```

`AppUser` 자체는 로그인/인증만 담당하고,  
실제 게임 데이터(닉네임, 재화 등)는 `User` 테이블에서 관리한다.  
`AccountId`가 그 연결 고리다.

**UserType 필드**

`"User"` 또는 `"Admin"` 문자열로 구분한다.  
이 값이 로그인 시 `actor_type` 클레임에 그대로 들어간다.

---

## 3. 로그인 시 — Claims 생성

`AppUserClaimsPrincipalFactory`가 로그인 시 호출되어 ClaimsPrincipal에 클레임을 추가한다.

```csharp
protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser appUser)
{
    var identity = await base.GenerateClaimsAsync(appUser);  // 기본 클레임 생성

    // 1. actor_type 추가
    identity.AddClaim(new Claim("actor_type", appUser.UserType));

    // 2. actor_id 추가 — UserType에 따라 조회 대상이 다름
    if (appUser.UserType == "User")
    {
        var domainUser = await _db.Users
            .FirstOrDefaultAsync(u => u.AccountId == appUser.Id);

        if (domainUser != null)
            identity.AddClaim(new Claim("actor_id", domainUser.UserId.ToString()));
    }
    else if (appUser.UserType == "Admin" && appUser.AdminId.HasValue)
    {
        identity.AddClaim(new Claim("actor_id", appUser.AdminId.Value.ToString()));
    }

    return identity;
}
```

**UserType별 actor_id 조회 방식 차이**

| UserType | actor_id 출처 | 조회 방법 |
|----------|--------------|---------|
| `"User"` | `User.UserId` | `Users` 테이블에서 `AccountId == appUser.Id`로 조회 |
| `"Admin"` | `Admin.AdminId` | `appUser.AdminId` 직접 사용 |

User는 `AppUser.Id → User.UserId` 변환이 필요하지만,  
Admin은 `AppUser.AdminId`에 이미 값이 있어서 DB 조회 없이 바로 사용한다.

**최종 클레임 구성**

```
actor_id   : 도메인 기준 ID (UserId 또는 AdminId)
actor_type : "User" | "Admin"
+ Identity 기본 클레임 (NameIdentifier, Name, Role 등)
```

---

## 4. 요청 시 — SmartAuth 스킴 선택

이 프로젝트는 **JWT와 Cookie 인증을 동시에 지원**한다.  
요청 특성에 따라 자동으로 어떤 방식을 쓸지 선택하는 `SmartAuth` 정책 스킴이 있다.

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme          = "SmartAuth";
    options.DefaultChallengeScheme = "SmartAuth";
})
.AddPolicyScheme("SmartAuth", "SmartAuth", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // 1) Authorization: Bearer ... 헤더가 있으면 JWT
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            return JwtBearerDefaults.AuthenticationScheme;

        // 2) /api 경로면 JWT
        if (context.Request.Path.StartsWithSegments("/api"))
            return JwtBearerDefaults.AuthenticationScheme;

        // 3) 그 외 UI 페이지는 Cookie
        return IdentityConstants.ApplicationScheme;
    };
});
```

**선택 기준**

| 조건 | 사용 스킴 | 대상 |
|------|---------|------|
| `Authorization: Bearer ...` 헤더 있음 | JWT | 외부 API 클라이언트 |
| 경로가 `/api`로 시작 | JWT | REST API |
| 그 외 | Cookie | 브라우저 UI (`/board`, `/admin/ui`) |

**왜 이 방식이 필요한가**

브라우저로 관리 페이지를 쓸 때는 쿠키가 자동으로 첨부되므로 Cookie 방식이 자연스럽다.  
외부 API 클라이언트(모바일, 게임 서버 등)는 쿠키 관리가 어려우니 JWT를 사용한다.  
`SmartAuth`가 이 둘을 하나의 인증 파이프라인으로 통합한다.

**JWT 검증 설정**

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer           = true,
    ValidateAudience         = true,
    ValidateLifetime         = true,
    ValidateIssuerSigningKey = true,
    ClockSkew                = TimeSpan.Zero   // 만료 시각 오차 허용 없음
};
```

`ClockSkew = TimeSpan.Zero` — 기본값은 5분 오차를 허용하지만 여기서는 0으로 설정해 토큰 만료를 정확히 적용한다.

**로그인 리다이렉트 경로 분기**

```csharp
options.Events.OnRedirectToLogin = context =>
{
    // /admin 또는 /AdminUi 경로에서 막히면
    if (context.Request.Path.StartsWithSegments("/admin") || ...)
    {
        context.Response.Redirect("/admin/login");  // Admin 로그인 페이지
        return Task.CompletedTask;
    }
    context.Response.Redirect("/account/login");    // 일반 유저 로그인 페이지
    return Task.CompletedTask;
};
```

미인증 상태에서 URL에 따라 다른 로그인 페이지로 보낸다.

---

## 5. IUserContext — Controller/Service에서 사용자 식별

```csharp
public interface IUserContext
{
    bool      IsAuthenticated { get; }
    long      ActorId         { get; }  // UserId 또는 AdminId
    ActorType ActorType       { get; }  // USER | ADMIN
}
```

Controller나 Service가 `HttpContext`나 `ClaimsPrincipal`을 직접 다루지 않도록 추상화한 인터페이스다.

**HttpUserContext 구현**

```csharp
public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public bool IsAuthenticated
        => _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public long ActorId
    {
        get
        {
            if (!IsAuthenticated)
                throw new UnauthorizedException("User is not authenticated.");
            return user.GetActorIdOrThrow();  // 클레임에서 파싱
        }
    }

    public ActorType ActorType
    {
        get
        {
            if (!IsAuthenticated)
                return ActorType.USER;  // 미인증이면 USER 기본값
            return user.GetActorTypeOrDefault(ActorType.USER);
        }
    }
}
```

**ActorId vs ActorType 미인증 처리 차이**

| 속성 | 미인증 시 |
|------|---------|
| `ActorId` | `UnauthorizedException` 던짐 |
| `ActorType` | `ActorType.USER` 기본값 반환 |

`ActorId`는 인증 없이 사용할 수 없지만,  
`ActorType`은 `EnsureCanDelete()` 같은 곳에서 미인증 상태에도 안전하게 참조할 수 있다.

**DI 등록 (Program.cs)**

```csharp
builder.Services.AddScoped<IUserContext, HttpUserContext>();
```

`Scoped`로 등록되어 HTTP 요청 1개당 1개의 인스턴스가 생성된다.

---

## 6. ClaimsPrincipalExtensions — Claims 파싱

```csharp
public static long GetActorIdOrThrow(this ClaimsPrincipal user)
{
    // 우선순위 1: actor_id 클레임
    var value = user.FindFirstValue("actor_id");

    // 우선순위 2: 하위 호환 (NameIdentifier / sub)
    if (string.IsNullOrWhiteSpace(value))
    {
        value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");
    }

    if (!long.TryParse(value, out var id))
        throw new UnauthorizedException("Invalid actor id claim.");

    return id;
}

public static ActorType GetActorTypeOrDefault(this ClaimsPrincipal user, ActorType defaultValue = ActorType.USER)
{
    // 우선순위 1: actor_type
    // 우선순위 2: user_type (과거 호환)
    var value = user.FindFirstValue("actor_type")
                ?? user.FindFirstValue("user_type");

    return Enum.TryParse<ActorType>(value, ignoreCase: true, out var result)
        ? result
        : defaultValue;
}
```

**하위 호환 우선순위가 있는 이유**

과거 클레임 키(`NameIdentifier`, `sub`, `user_type`)를 쓰던 버전과 공존하기 위해서다.  
새 방식(`actor_id`, `actor_type`)으로 통일되면 하위 호환 코드는 제거 가능하다.

**`ignoreCase: true` — 대소문자 무관 파싱**

`"user"`, `"User"`, `"USER"` 모두 `ActorType.USER`로 파싱된다.  
클레임 값이 대소문자 혼용으로 들어와도 안전하다.

---

## 7. 권한 정책 — Authorize vs AdminOnly

**정책 정의 (Program.cs)**

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Admin", "SuperAdmin");
    });
});
```

`AdminOnly` 정책은 Role이 `"Admin"` 또는 `"SuperAdmin"`인 사용자만 통과한다.

**사용 위치별 비교**

```csharp
// 일반 유저 + Admin 모두 접근 가능 (인증만 필요)
[Authorize]
public async Task<IActionResult> Create() { ... }

// Admin 또는 SuperAdmin만 접근 가능
[Authorize(Policy = "AdminOnly")]
public sealed class AdminPostController : ControllerBase { ... }

// 클래스는 일반 인증, 특정 메서드만 Admin
[Authorize]
public sealed class GachaController : ControllerBase
{
    [Authorize(Roles = "Admin")]  // 메서드 레벨 추가
    [HttpGet("api/admin/gacha/pools")]
    public async Task<IActionResult> GetPools() { ... }
}
```

**권한 실패 HTTP 응답**

| 상황 | 응답 |
|------|------|
| 비인증 (토큰/쿠키 없음) | 401 Unauthorized |
| 인증됐지만 Role 없음 | 403 Forbidden |

---

## 8. 예외 처리 — ExceptionMiddleware

인증/인가 과정에서 발생하는 예외를 포함해 모든 예외를 한 곳에서 처리한다.

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try { await _next(context); }
    catch (RequestValidationException ex) { await HandleValidationException(context, ex); }
    catch (AppException ex)              { await HandleAppException(context, ex); }
    catch (Exception ex)                 { await HandleUnknownException(context, ex); }
}
```

**예외 → HTTP 상태 코드 매핑**

| 예외 | 상태 코드 | 설명 |
|------|---------|------|
| `RequestValidationException` | 422 | 요청 필드 유효성 실패 |
| `AppException` (하위 클래스) | `ex.StatusCode` | 각 예외가 직접 보유 |
| `UnauthorizedException` | 401 | 인증 필요 |
| `ForbiddenException` | 403 | 권한 없음 |
| `DomainException` | 400 | 비즈니스 규칙 위반 |
| 그 외 `Exception` | 500 | 서버 내부 오류 |

**미들웨어 파이프라인 등록 순서**

```csharp
app.UseRouting();
app.UseMiddleware<ExceptionMiddleware>();  // ← 예외 처리 먼저
app.UseAuthentication();                  // ← 그 다음 인증
app.UseAuthorization();                   // ← 마지막 인가
```

`ExceptionMiddleware`가 `UseAuthentication` 앞에 있어서  
인증 과정에서 발생하는 예외도 잡을 수 있다.

---

## 9. 테스트 인증 — TestAuthHandler

`#if DEBUG` 블록 안에서만 컴파일된다. 운영 환경에는 포함되지 않는다.

```csharp
public sealed class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 요청 헤더로 테스트 Actor 변경 가능
        // X-Test-ActorType: User | Admin
        // X-Test-ActorId: 1
        var actorType = Request.Headers.TryGetValue("X-Test-ActorType", out var at)
            ? at.ToString()
            : Options.DefaultActorType;  // 기본값: "User"

        var claims = new List<Claim>
        {
            new Claim("actor_id",   actorId.ToString()),
            new Claim("actor_type", actorType),
            new Claim(ClaimTypes.Role, actorType == "Admin" ? "Admin" : "User")
        };

        // 항상 인증 성공으로 처리
        return Task.FromResult(AuthenticateResult.Success(...));
    }
}
```

**테스트 시 사용법**

```http
GET /api/gacha/currency
X-Test-ActorType: Admin
X-Test-ActorId: 42
```

헤더 2개만 추가하면 특정 Actor로 인증된 것처럼 동작한다.  
실제 로그인 없이 API를 테스트할 수 있다.

---

## 10. 전체 구성 요소 연결도

```
[로그인 시]

  UserManager.SignInAsync()
       │
       ▼
  AppUserClaimsPrincipalFactory.GenerateClaimsAsync()
       ├─ actor_type = AppUser.UserType
       ├─ actor_id   = User.UserId   (UserType == "User")
       │              Admin.AdminId  (UserType == "Admin")
       └─ + Identity 기본 클레임 (NameIdentifier, Role 등)
       │
       ▼
  Cookie 저장 또는 JWT 토큰 발급


[요청 시]

  HTTP Request
       │
       ▼
  SmartAuth.ForwardDefaultSelector
       ├─ Bearer 헤더 / /api 경로  → JwtBearer 검증
       └─ 그 외                    → Identity Cookie 검증
       │
       ▼
  ClaimsPrincipal 복원
       │
       ▼
  [Authorize] / [Authorize(Policy="AdminOnly")]
       │
       ▼
  Controller
       │
       ▼
  IUserContext (HttpUserContext)
       ├─ ActorId   → ClaimsPrincipalExtensions.GetActorIdOrThrow()
       │               └─ "actor_id" 클레임 파싱
       └─ ActorType → ClaimsPrincipalExtensions.GetActorTypeOrDefault()
                       └─ "actor_type" 클레임 파싱

  [예외 발생 시]
  ExceptionMiddleware → HTTP 상태 코드 변환 → JSON 응답
```

---

## 요약 — 핵심 설계 포인트

| 포인트 | 내용 |
|--------|------|
| 두 인증 방식 통합 | `SmartAuth`가 JWT/Cookie를 경로/헤더 기준으로 자동 선택 |
| Identity ↔ Domain 분리 | `AppUser`는 인증만, 게임 데이터는 `User`/`Admin` 테이블이 담당 |
| Claims 추상화 | `IUserContext`로 Controller/Service가 `ClaimsPrincipal` 직접 접근 안 함 |
| 하위 호환 | `actor_id` 없으면 `NameIdentifier`/`sub`로 fallback |
| Admin 권한 | `RequireRole("Admin", "SuperAdmin")` — SuperAdmin도 포함 |
| 예외 → HTTP 변환 | `ExceptionMiddleware` 한 곳에서 전체 처리 |
