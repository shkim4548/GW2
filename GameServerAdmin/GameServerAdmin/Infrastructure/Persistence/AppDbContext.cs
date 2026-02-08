using GameServerAdmin.Domain.Accounts;
using GameServerAdmin.Domain.Admins;
using GameServerAdmin.Domain.Comments;
using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Domain.Notices;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Reflection.Emit;

namespace GameServerAdmin.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Notice> Notices => Set<Notice>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>().ToTable("app_users");
            modelBuilder.Entity<AppRole>().ToTable("app_roles");
            modelBuilder.Entity<IdentityUserRole<long>>().ToTable("app_user_roles");
            modelBuilder.Entity<IdentityUserClaim<long>>().ToTable("app_user_claims");
            modelBuilder.Entity<IdentityUserLogin<long>>().ToTable("app_user_logins");
            modelBuilder.Entity<IdentityUserToken<long>>().ToTable("app_user_tokens");
            modelBuilder.Entity<IdentityRoleClaim<long>>().ToTable("app_role_claims");
             
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly
            );
        }
    }
}
