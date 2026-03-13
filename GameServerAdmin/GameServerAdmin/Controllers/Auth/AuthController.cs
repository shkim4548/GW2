using GameServerAdmin.Domain.Accounts;
using GameServerAdmin.Domain.Admins;
using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Domain.Users;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GameServerAdmin.Controllers.Auth
{
    [ApiController]
    [Tags("Auth")]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration, AppDbContext dbContext, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// JWT 로그인 (일반 유저 + 관리자 공용)
        /// AppUser.UserType == "Admin" 이면 Admin 도메인 기준으로 토큰을 발급한다.
        /// 그렇지 않으면 User 도메인 기준으로 토큰을 발급한다.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) Identity(AppUser) 조회
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {UserName}", request.UserName);
                return Unauthorized();
            }

            // 2) 비밀번호 검증
            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Login failed - wrong password: {UserName}", request.UserName);
                return Unauthorized();
            }

            // 3) 역할(Role) 조회
            var roles = await _userManager.GetRolesAsync(user);

            // 4) 도메인 기준 Actor(User 또는 Admin) 결정
            long actorId;
            string actorType;

            if (string.Equals(user.UserType, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // ==============================
                // Admin 로그인 처리
                // ==============================

                if (user.AdminId == null)
                {
                    // AppUser.AdminId가 비어 있으면 Admin 도메인 자동 생성
                    var admin = new Admin
                    {
                        LoginId = user.UserName ?? $"admin_{user.Id}",
                        PasswordHash = string.Empty, // 인증에는 사용하지 않고, NOT NULL 제약만 맞춘다.
                        Role = "Super",              // 기본 권한명 (필요시 변경 가능)
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _dbContext.Admins.Add(admin);
                    await _dbContext.SaveChangesAsync();

                    // AppUser와 Admin 연결
                    user.AdminId = admin.AdminId;
                    await _userManager.UpdateAsync(user);

                    actorId = admin.AdminId;
                }
                else
                {
                    var admin = await _dbContext.Admins
                        .FirstOrDefaultAsync(a => a.AdminId == user.AdminId.Value);

                    if (admin == null || !admin.IsActive)
                    {
                        _logger.LogWarning("Login failed - admin inactive or not found: {UserName}", user.UserName);
                        return Unauthorized();
                    }

                    admin.LastLoginAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    actorId = admin.AdminId;
                }

                actorType = "ADMIN";
            }
            else
            {
                // ==============================
                // 일반 유저 로그인 처리
                // (게임 플레이어용 Actor = User)
                // ==============================

                actorType = "USER";

                if (user.AccountId == null)
                {
                    // Account + User 도메인 자동 생성
                    var account = new Account
                    {
                        LoginId = user.UserName ?? $"user_{user.Id}",
                        PasswordHash = string.Empty, // 실제 로그인은 Identity가 처리
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow,
                        IsBanned = false
                    };

                    _dbContext.Accounts.Add(account);
                    await _dbContext.SaveChangesAsync();

                    var domainUser = new User
                    {
                        AccountId = account.AccountId,
                        Nickname = user.NickName ?? user.UserName ?? $"Player_{account.AccountId}",
                        Level = 1,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow,
                        Status = UserStatus.Active,
                    };

                    _dbContext.Users.Add(domainUser);
                    await _dbContext.SaveChangesAsync();

                    user.AccountId = account.AccountId;
                    await _userManager.UpdateAsync(user);

                    actorId = domainUser.UserId;
                }
                else
                {
                    var accountId = user.AccountId.Value;

                    var domainUser = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.AccountId == accountId);

                    if (domainUser == null)
                    {
                        domainUser = new User
                        {
                            AccountId = accountId,
                            Nickname = user.NickName ?? user.UserName ?? $"Player_{accountId}",
                            Level = 1,
                            CreatedAt = DateTime.UtcNow,
                            LastLoginAt = DateTime.UtcNow,
                            Status = UserStatus.Active,
                        };

                        _dbContext.Users.Add(domainUser);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        domainUser.LastLoginAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();
                    }

                    actorId = domainUser.UserId;
                }
            }

            // 5) 클레임 구성
            var claims = new List<Claim>
            {
                // Identity(AppUser) 기준 키
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),

                // 표시용
                new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),

                // 도메인 Actor(User/Admin)
                new("actor_id", actorId.ToString()),
                new("actor_type", actorType),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // 6) 토큰 생성
            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Jwt:Key is not configured.");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(
                _configuration.GetValue<int>("Jwt:ExpireHours", 24));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

            _logger.LogInformation("Login success: {UserName} ActorType={ActorType} ActorId={ActorId}", user.UserName, actorType, actorId);

            // 7) 응답
            return Ok(new LoginResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAt,
                UserName = user.UserName ?? string.Empty,
                Roles = roles.ToArray()
            });
        }
    }
}