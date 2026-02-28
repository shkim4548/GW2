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
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration configuration,
            AppDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        /// <summary>
        /// JWT 로그인
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) Identity(AppUser) 기준으로 사용자 조회
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
                return Unauthorized();

            // 2) 패스워드 검증
            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: false);

            if (!signInResult.Succeeded)
                return Unauthorized();

            // 3) 역할 조회
            var roles = await _userManager.GetRolesAsync(user);

            // 4) 도메인 기준 Actor(User 또는 Admin) 조회
            long actorId;
            string actorType;

            if (string.Equals(user.UserType, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Admin 계정
                if (user.AdminId == null)
                    return Unauthorized();

                var admin = await _dbContext.Admins
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AdminId == user.AdminId.Value);

                if (admin == null || !admin.IsActive)
                    return Unauthorized();

                actorId = admin.AdminId;
                actorType = "Admin";
            }
            else
            {
                // 기본값: 일반 유저
                if (user.AccountId == null)
                    return Unauthorized();

                var domainUser = await _dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.AccountId == user.AccountId.Value);

                if (domainUser == null)
                    return Unauthorized();

                actorId = domainUser.UserId;
                actorType = "User";
            }

            // 5) 클레임 구성
            var claims = new List<Claim>
            {
                // Identity 기준 키(AppUser.Id) - 기술적인 사용자 식별자
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),

                // 표시용
                new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),

                // 도메인 기준 Actor(User/Admin)
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