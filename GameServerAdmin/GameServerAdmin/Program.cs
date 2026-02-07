using GameServerAdmin.Application.Comments.Admin;
using GameServerAdmin.Application.Comments.Public;
using GameServerAdmin.Application.Notice;
using GameServerAdmin.Application.Posts;
using GameServerAdmin.Application.Posts.Public;
using GameServerAdmin.Application.Validation;
using GameServerAdmin.Controllers.MiddleWare;
using GameServerAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // DBContext 등록
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                    );
            });

            // Filter 등록
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ModelStateValidationFilter>();
            });

            // Controller 등록
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddScoped<PublicPostService>();
            builder.Services.AddScoped<PublicCommentService>();
            builder.Services.AddScoped<AdminPostService>();
            builder.Services.AddScoped<AdminCommentService>();
            builder.Services.AddScoped<PublicNoticeService>();
            builder.Services.AddScoped<AdminNoticeService>();

            var app = builder.Build();

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

                app.UseHttpsRedirection();
            //app.UseStaticFiles();

            //app.UseRouting();
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseAuthorization();
            app.MapControllers();
            //app.MapRazorPages();

            app.Run();
        }
    }
}
