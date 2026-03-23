using GameServerAdmin.Domain.Game.Gacha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Configurations.Gacha;

public class PlayerGachaInventoryConfiguration : IEntityTypeConfiguration<PlayerGachaInventory>
{
    public void Configure(EntityTypeBuilder<PlayerGachaInventory> builder)
    {
        builder.ToTable("player_gacha_inventory");

        builder.HasKey(e => e.PlayerGachaInventoryId);
        builder.Property(e => e.PlayerGachaInventoryId)
            .HasColumnName("player_gacha_inventory_id")
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.GachaItemId)
            .HasColumnName("gacha_item_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.AcquiredAt)
            .HasColumnName("acquired_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.GachaItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_player_gacha_inventory_user_id");

        builder.HasIndex(e => new { e.UserId, e.GachaItemId })
            .HasDatabaseName("IX_player_gacha_inventory_user_item");
    }
}
