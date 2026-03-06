using GameServerAdmin.Domain.Game.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class AdminCurrencyLogConfiguration : IEntityTypeConfiguration<AdminCurrencyLog>
    {
        public void Configure(EntityTypeBuilder<AdminCurrencyLog> builder)
        {
            builder.ToTable("admin_currency_log");

            builder.HasKey(x => x.AdminCurrencyLogId);

            builder.Property(x => x.AdminCurrencyLogId)
                .HasColumnName("admin_currency_log_id")
                .HasColumnType("bigint")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.AdminId)
                .HasColumnName("admin_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.CurrencyType)
                .HasColumnName("currency_type")
                .HasConversion<int>()
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.ChangeAmount)
                .HasColumnName("change_amount")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.BeforeAmount)
                .HasColumnName("before_amount")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.AfterAmount)
                .HasColumnName("after_amount")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.Reason)
                .HasColumnName("reason")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_admin_currency_log_user_id");

            builder.HasIndex(x => x.AdminId)
                .HasDatabaseName("IX_admin_currency_log_admin_id");
        }
    }
}