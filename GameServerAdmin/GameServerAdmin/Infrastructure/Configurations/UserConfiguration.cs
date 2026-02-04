using GameServerAdmin.Domain.Users;
using GameServerAdmin.Infrastructure.Configurations;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("user");

            builder.HasKey(e => e.UserId);
            builder.Property(e => e.UserId)
                .HasColumnName("user_id")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(e => e.Nickname)
                .HasColumnName("nickname")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Level)
                .HasColumnName("level")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasColumnType("text")
                .IsRequired();

            // 인덱스
            builder.HasIndex(e => e.AccountId)
                .HasDatabaseName("IX_user_account_id");

            builder.HasIndex(e => e.Nickname)
                .IsUnique()
                .HasDatabaseName("IX_user_nickname");
        }
    }
}