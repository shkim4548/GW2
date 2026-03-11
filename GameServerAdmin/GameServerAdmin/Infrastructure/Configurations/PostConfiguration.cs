using GameServerAdmin.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.ToTable("post");

            builder.HasKey(e => e.PostId);
            builder.Property(e => e.PostId)
                .HasColumnName("post_id")
                .HasColumnType("bigint")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.PostType)
                .HasColumnName("post_type")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Content)
                .HasColumnName("content")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(e => e.AuthorType)
                .HasColumnName("author_type")
                .HasMaxLength(20)
                .HasConversion<string>()   // ← enum을 string으로 저장/읽기
                .IsRequired();

            builder.Property(e => e.AuthorId)
                .HasColumnName("author_id")
                .HasColumnType("bigint")
                .IsRequired();


            builder.Property(e => e.AuthorName)
                .HasColumnName("author_name")
                .HasColumnType("varchar(100)") // 길이는 취향, 50~100 정도
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.DeletedAt)                 // ← 추가
                .HasColumnName("deleted_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("is_deleted")
                .IsRequired();

            // Post에 Notice 기능이 추가된 내용
            builder.Property(e => e.ViewCount)
                .HasColumnName("view_count")
                .IsRequired();

            builder.Property(e => e.IsCommentEnabled)
                .HasColumnName("is_comment_enabled")
                .IsRequired();

            // 인덱스
            builder.HasIndex(e => e.AuthorId)
                .HasDatabaseName("IX_post_author_id");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_post_created_at");

            builder.HasIndex(e => new { e.IsDeleted, e.PostType })
                .HasDatabaseName("IX_post_is_deleted_post_type");
        }
    }
}