using GameServerAdmin.Domain.Game.Gacha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Configurations.Gacha;

public class GachaPoolConfiguration : IEntityTypeConfiguration<GachaPool>
{
    public void Configure(EntityTypeBuilder<GachaPool> builder)
    {
        builder.ToTable("gacha_pool");

        builder.HasKey(e => e.GachaPoolId);
        builder.Property(e => e.GachaPoolId)
            .HasColumnName("gacha_pool_id")
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SinglePullCost)
            .HasColumnName("single_pull_cost")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(e => e.TenPullCost)
            .HasColumnName("ten_pull_cost")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(e => e.PityThreshold)
            .HasColumnName("pity_threshold")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(e => e.PityGrade)
            .HasColumnName("pity_grade")
            .HasColumnType("integer")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(e => e.StartAt)
            .HasColumnName("start_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.EndAt)
            .HasColumnName("end_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
