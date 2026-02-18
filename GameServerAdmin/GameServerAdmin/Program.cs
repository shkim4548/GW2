using GameServerAdmin.Application.Comments.Admin;
using GameServerAdmin.Application.Comments.Public;
using GameServerAdmin.Application.Notice;
using GameServerAdmin.Application.Posts;
using GameServerAdmin.Application.Posts.Public;
using GameServerAdmin.Application.Validation;
using GameServerAdmin.Controllers.MiddleWare;
using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GameServerAdmin
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // DBContext 등록
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                    );
            });

            // Identity 설정
            builder.Services.AddIdentity<AppUser, AppRole>(options =>
            {
                // password 정책
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                // Lockout 설정
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User 설정
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedEmail = false;
            }).AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // 쿠키 리다이렉트 경로
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.AccessDeniedPath = "/account/denied";

                // Admin UI로 들어가다 막히면 /admin/login으로 보내기
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/admin") || context.Request.Path.StartsWithSegments("/Admin") || context.Request.Path.Value?.StartsWith("/AdminUi", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        context.Response.Redirect("/admin/login");
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect("/account/login");
                    return Task.CompletedTask;
                };
            });

            // JWT + Cookie(Identity) 혼용: 요청에 따라 자동 선택
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "SmartAuth";
                options.DefaultChallengeScheme = "SmartAuth";
            })
            .AddPolicyScheme("SmartAuth", "SmartAuth", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    // 1) Authorization: Bearer ... 가 있으면 JWT
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
                        return JwtBearerDefaults.AuthenticationScheme;

                    // 2) /api 로 시작하면 JWT
                    if (context.Request.Path.StartsWithSegments("/api"))
                        return JwtBearerDefaults.AuthenticationScheme;

                    // 3) 그 외(UI)는 Identity Cookie
                    return IdentityConstants.ApplicationScheme;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                {
                    policy.RequireRole("Admin", "SuperAdmin");
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // MVC (API + View) + Global Filter
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<ModelStateValidationFilter>();
            });

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddScoped<IPublicPostService, PublicPostService>();
            builder.Services.AddScoped<IPublicCommentService, PublicCommentService>();
            builder.Services.AddScoped<IAdminPostService, AdminPostService>();
            builder.Services.AddScoped<IAdminCommentService, AdminCommentService>();
            builder.Services.AddScoped<IPublicNoticeService, PublicNoticeService>();
            builder.Services.AddScoped<IAdminNoticeService, AdminNoticeService>();

            var app = builder.Build();

            // DB 초기화
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
                var userManager = services.GetRequiredService<UserManager<AppUser>>();

                await DbInitializer.SeedRolesAsync(roleManager);
                await DbInitializer.SeedAdminUserAsync(userManager, roleManager);
                await DbInitializer.SeedDefaultUserAsync(userManager, roleManager);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Https 사용
            app.UseHttpsRedirection();
            // wwwroot, css, js등 정적 파일 사용
            app.UseStaticFiles();

            app.UseRouting();
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            
            // MVC View용 기본 라우트 (Board/Index를 기본으로
            app.MapControllerRoute
                (name: "default",
                pattern: "{controller=Board}/{action=Index}/{id?}"
                );
            app.MapControllers();
            //app.MapRazorPages();

            app.Run();
        }
    }
}
