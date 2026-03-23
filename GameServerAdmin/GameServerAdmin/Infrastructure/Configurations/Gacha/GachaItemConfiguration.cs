using GameServerAdmin.Domain.Game.Gacha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Configurations.Gacha;

public class GachaItemConfiguration : IEntityTypeConfiguration<GachaItem>
{
    public void Configure(EntityTypeBuilder<GachaItem> builder)
    {
        builder.ToTable("gacha_item");

        builder.HasKey(e => e.GachaItemId);
        builder.Property(e => e.GachaItemId)
            .HasColumnName("gacha_item_id")
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Grade)
            .HasColumnName("grade")
            .HasColumnType("integer")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.DuplicateCompensation)
            .HasColumnName("duplicate_compensation")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
    }
}
