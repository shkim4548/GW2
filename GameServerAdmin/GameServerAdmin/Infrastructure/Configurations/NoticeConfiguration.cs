using GameServerAdmin.Domain.Notices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class NoticeConfiguration : IEntityTypeConfiguration<Notice>
    {
        public void Configure(EntityTypeBuilder<Notice> builder)
        {
            builder.ToTable("notice");

            // 기본키
            builder.HasKey(e => e.NoticeId);
            builder.Property(e => e.NoticeId)
                .HasColumnName("notice_id")
                .HasColumnType("bigint")
                .ValueGeneratedOnAdd();

            // 컬럼 매핑
            builder.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Content)
                .HasColumnName("content")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(e => e.AdminId)
                .HasColumnName("admin_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(e => e.Category)
                .HasColumnName("category")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(e => e.Priority)
                .HasColumnName("priority")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(e => e.IsPinned)
                .HasColumnName("is_pinned")
                .IsRequired();

            builder.Property(e => e.DisplayStartAt)
                .HasColumnName("display_start_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.DisplayEndAt)
                .HasColumnName("display_end_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.ViewCount)
                .HasColumnName("view_count")
                .IsRequired();

            builder.Property(e => e.IsCommentEnabled)
                .HasColumnName("is_comment_enabled")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            // 인덱스
            builder.HasIndex(e => e.AdminId)
                .HasDatabaseName("IX_notice_admin_id");

            builder.HasIndex(e => e.Category)
                .HasDatabaseName("IX_notice_category");

            builder.HasIndex(e => e.Status)
                .HasDatabaseName("IX_notice_status");

            builder.HasIndex(e => new { e.Status, e.IsPinned })
                .HasDatabaseName("IX_notice_status_is_pinned");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_notice_created_at");

            builder.HasIndex(e => new { e.DisplayStartAt, e.DisplayEndAt })
                .HasDatabaseName("IX_notice_display_period");
        }
    }
}