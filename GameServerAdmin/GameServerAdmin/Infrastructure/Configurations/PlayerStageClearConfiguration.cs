using GameServerAdmin.Domain.Game.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class PlayerStageClearConfiguration : IEntityTypeConfiguration<PlayerStageClear>
    {
        public void Configure(EntityTypeBuilder<PlayerStageClear> builder)
        {
            builder.ToTable("player_stage_clear");

            builder.HasKey(e => e.PlayerStageClearId);

            builder.Property(e => e.PlayerStageClearId)
                .HasColumnName("player_stage_clear_id")
                .HasColumnType("bigint");

            builder.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(e => e.StageId)
                .HasColumnName("stage_id")
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(e => e.ClearedAt)
                .HasColumnName("cleared_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasIndex(e => new { e.UserId, e.StageId })
                .HasDatabaseName("IX_player_stage_clear_user_stage");
        }
    }
}