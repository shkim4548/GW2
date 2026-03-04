using GameServerAdmin.Domain.Game.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class PlayerCurrencyConfiguration : IEntityTypeConfiguration<PlayerCurrency>
    {
        public void Configure(EntityTypeBuilder<PlayerCurrency> builder)
        {
            builder.ToTable("player_currency");

            builder.HasKey(x => x.PlayerCurrencyId);

            builder.Property(x => x.PlayerCurrencyId)
                .HasColumnName("player_currency_id")
                .HasColumnType("bigint");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.CurrencyType)
                .HasColumnName("currency_type")
                .HasConversion<int>()    // enum ⇔ int
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnName("amount")
                .HasColumnType("bigint")
                .IsRequired();

            // 낙관적 동시성 토큰 매핑
            builder.Property(x => x.RowVersion)
                .HasColumnName("row_version")
                .IsRowVersion() // ConcurrencyToken + byte[] timestamp
                .IsConcurrencyToken();

            builder.HasIndex(x => new { x.UserId, x.CurrencyType })
                   .HasDatabaseName("IX_player_currency_user_currency")
                   .IsUnique();
        }
    }
}