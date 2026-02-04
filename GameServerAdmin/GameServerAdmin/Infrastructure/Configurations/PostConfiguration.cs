using GameServerAdmin.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            // 테이블명
            builder.ToTable("post");

            // 기본키
            builder.HasKey(e => e.PostId);
            builder.Property(e => e.PostId)
                .HasColumnName("post_id")
                .ValueGeneratedOnAdd();

            // 컬럼 매핑
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
                .IsRequired();

            builder.Property(e => e.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("is_deleted")
                .IsRequired();

            // 인덱스 (성능 최적화용 - 추가 권장)
            builder.HasIndex(e => e.AuthorId)
                .HasDatabaseName("IX_post_author_id");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_post_created_at");

            builder.HasIndex(e => new { e.IsDeleted, e.PostType })
                .HasDatabaseName("IX_post_is_deleted_post_type");
        }
    }
}