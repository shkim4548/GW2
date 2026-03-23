using GameServerAdmin.Domain.Game.Gacha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Configurations.Gacha;

public class PlayerGachaPityConfiguration : IEntityTypeConfiguration<PlayerGachaPity>
{
    public void Configure(EntityTypeBuilder<PlayerGachaPity> builder)
    {
        builder.ToTable("player_gacha_pity");

        builder.HasKey(e => e.PlayerGachaPityId);
        builder.Property(e => e.PlayerGachaPityId)
            .HasColumnName("player_gacha_pity_id")
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.GachaPoolId)
            .HasColumnName("gacha_pool_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.PullCount)
            .HasColumnName("pull_count")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.GachaPoolId })
            .IsUnique()
            .HasDatabaseName("IX_player_gacha_pity_user_pool");
    }
}
