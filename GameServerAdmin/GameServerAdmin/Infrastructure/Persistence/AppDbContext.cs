using GameServerAdmin.Domain.Accounts;
using GameServerAdmin.Domain.Admins;
using GameServerAdmin.Domain.Comments;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Domain.Users;
using Microsoft.EntityFrameworkCore;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly
            );
        }
    }
}
