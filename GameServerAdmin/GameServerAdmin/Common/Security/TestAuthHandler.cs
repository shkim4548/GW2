#if DEBUG
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GameServerAdmin.Common.Security.TestAuth
{
    public sealed class TestAuthOptions : AuthenticationSchemeOptions
    {
        public long DefaultActorId { get; set; } = 1;
        public string DefaultActorType { get; set; } = "User"; // "Admin"도 가능
        public string DefaultName { get; set; } = "TestActor";
    }

    public sealed class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<TestAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // (선택) 요청 헤더로 테스트 Actor 변경 가능
            // X-Test-ActorType: User | Admin
            // X-Test-ActorId: 1
            var actorType = Request.Headers.TryGetValue("X-Test-ActorType", out var at)
                ? at.ToString()
                : Options.DefaultActorType;

            var actorId = Options.DefaultActorId;
            if (Request.Headers.TryGetValue("X-Test-ActorId", out var aid) && long.TryParse(aid.ToString(), out var parsed))
                actorId = parsed;

            // ✅ 레포에서 이미 쓰는 claim 키: actor_id / actor_type
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
                new Claim(ClaimTypes.Name, Options.DefaultName),

                new Claim("actor_id", actorId.ToString()),
                new Claim("actor_type", actorType),
            };

            // (선택) Role 기반 Authorize(Roles=...)가 있으면 추가
            if (string.Equals(actorType, "Admin", StringComparison.OrdinalIgnoreCase))
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            else
                claims.Add(new Claim(ClaimTypes.Role, "User"));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, SchemeName)));
        }
    }
}
#endif