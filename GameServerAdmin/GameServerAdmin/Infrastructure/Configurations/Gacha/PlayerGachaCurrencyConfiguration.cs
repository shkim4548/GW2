using GameServerAdmin.Domain.Game.Gacha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Configurations.Gacha;

public class PlayerGachaCurrencyConfiguration : IEntityTypeConfiguration<PlayerGachaCurrency>
{
    public void Configure(EntityTypeBuilder<PlayerGachaCurrency> builder)
    {
        builder.ToTable("player_gacha_currency");

        builder.HasKey(e => e.UserId);
        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("bigint")
            .ValueGeneratedNever();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("integer")
            .IsRequired();
    }
}
