using GameServerAdmin.Domain.Comments;
using GameServerAdmin.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServerAdmin.Infrastructure.Persistence.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            // 테이블명 (snake_case 일관성)
            builder.ToTable("comment");

            // 기본키
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("comment_id")
                .ValueGeneratedOnAdd();

            // 컬럼 매핑
            builder.Property(e => e.PostId)
                .HasColumnName("post_id")
                .IsRequired();

            builder.Property(e => e.ParentCommentId)
                .HasColumnName("parent_comment_id")
                .IsRequired(false);

            builder.Property(e => e.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            builder.Property(e => e.Content)
                .HasColumnName("content")
                .HasColumnType("text")
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<int>()
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

            // 외래키 관계
            builder.HasOne<Post>()
                .WithMany()
                .HasForeignKey(e => e.PostId)
                .HasConstraintName("FK_comment_post")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Comment>()
                .WithMany()
                .HasForeignKey(e => e.ParentCommentId)
                .HasConstraintName("FK_comment_parent_comment")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // 인덱스
            builder.HasIndex(e => e.PostId)
                .HasDatabaseName("IX_comment_post_id");

            builder.HasIndex(e => e.ParentCommentId)
                .HasDatabaseName("IX_comment_parent_comment_id");

            builder.HasIndex(e => e.AuthorId)
                .HasDatabaseName("IX_comment_author_id");

            builder.HasIndex(e => new { e.PostId, e.Status })
                .HasDatabaseName("IX_comment_post_id_status");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_comment_created_at");
        }
    }
}