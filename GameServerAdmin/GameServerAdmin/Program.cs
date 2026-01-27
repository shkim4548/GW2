
using GameServerAdmin.Common.Filters;
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
                options.Filters.Add<HttpExceptionFilter>();
            });

            // Controller 등록
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add services to the container.
            builder.Services.AddRazorPages();

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

            app.UseAuthorization();
            app.MapControllers();
            //app.MapRazorPages();

            app.Run();
        }
    }
}
