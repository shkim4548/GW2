using GameServerAdmin.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("account");

            builder.HasKey(e => e.AccountId);
            builder.Property(e => e.AccountId)
                .HasColumnName("accound_id")  // 오타 유지 (DB와 일치)
                .HasColumnType("bigint")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.LoginId)
                .HasColumnName("login_id")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.IsBanned)
                .HasColumnName("is_banned")
                .IsRequired();

            builder.HasIndex(e => e.LoginId)
                .IsUnique()
                .HasDatabaseName("IX_account_login_id");
        }
    }
}