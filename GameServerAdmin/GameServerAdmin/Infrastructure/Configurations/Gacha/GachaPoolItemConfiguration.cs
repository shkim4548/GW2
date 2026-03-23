using GameServerAdmin.Domain.Game.Gacha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Configurations.Gacha;

public class GachaPoolItemConfiguration : IEntityTypeConfiguration<GachaPoolItem>
{
    public void Configure(EntityTypeBuilder<GachaPoolItem> builder)
    {
        builder.ToTable("gacha_pool_item");

        builder.HasKey(e => e.GachaPoolItemId);
        builder.Property(e => e.GachaPoolItemId)
            .HasColumnName("gacha_pool_item_id")
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.GachaPoolId)
            .HasColumnName("gacha_pool_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.GachaItemId)
            .HasColumnName("gacha_item_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.Weight)
            .HasColumnName("weight")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(e => e.Pool)
            .WithMany()
            .HasForeignKey(e => e.GachaPoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.GachaItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.GachaPoolId)
            .HasDatabaseName("IX_gacha_pool_item_pool_id");
    }
}
