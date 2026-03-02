using GameServerAdmin.Domain.Game.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class StageConfiguration : IEntityTypeConfiguration<Stage>
    {
        public void Configure(EntityTypeBuilder<Stage> builder)
        {
            builder.ToTable("stage");

            builder.HasKey(e => e.StageId);

            builder.Property(e => e.StageId)
                .HasColumnName("stage_id")
                .HasColumnType("integer");

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(e => e.RequiredStamina)
                .HasColumnName("required_stamina")
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(e => e.RewardGold)
                .HasColumnName("reward_gold")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(e => e.RewardGem)
                .HasColumnName("reward_gem")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(e => e.IsEnabled)
                .HasColumnName("is_enabled")
                .HasColumnType("boolean")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        }
    }
}