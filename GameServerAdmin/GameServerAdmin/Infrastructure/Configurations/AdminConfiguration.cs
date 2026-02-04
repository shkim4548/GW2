using GameServerAdmin.Domain.Admins;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class AdminConfiguration : IEntityTypeConfiguration<Admin>
    {
        public void Configure(EntityTypeBuilder<Admin> builder)
        {
            builder.ToTable("admin");

            builder.HasKey(e => e.AdminId);
            builder.Property(e => e.AdminId)
                .HasColumnName("admin_id")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.LoginId)
                .HasColumnName("login_id")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(e => e.Role)
                .HasColumnName("role")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            // 인덱스
            builder.HasIndex(e => e.LoginId)
                .IsUnique()
                .HasDatabaseName("IX_admin_login_id");
        }
    }
}