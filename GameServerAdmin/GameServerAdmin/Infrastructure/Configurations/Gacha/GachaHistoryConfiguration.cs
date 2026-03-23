using GameServerAdmin.Domain.Game.Gacha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Configurations.Gacha;

public class GachaHistoryConfiguration : IEntityTypeConfiguration<GachaHistory>
{
    public void Configure(EntityTypeBuilder<GachaHistory> builder)
    {
        builder.ToTable("gacha_history");

        builder.HasKey(e => e.GachaHistoryId);
        builder.Property(e => e.GachaHistoryId)
            .HasColumnName("gacha_history_id")
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

        builder.Property(e => e.GachaItemId)
            .HasColumnName("gacha_item_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.PullType)
            .HasColumnName("pull_type")
            .HasColumnType("integer")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.IsDuplicate)
            .HasColumnName("is_duplicate")
            .IsRequired();

        builder.Property(e => e.CompensationAmount)
            .HasColumnName("compensation_amount")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(e => e.PulledAt)
            .HasColumnName("pulled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_gacha_history_user_id");

        builder.HasIndex(e => e.PulledAt)
            .HasDatabaseName("IX_gacha_history_pulled_at");
    }
}
