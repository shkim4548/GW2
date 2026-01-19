using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain
{
    [Table("post")]
    public class Post
    {
        [Key]
        [Column("post_id")]
        public int PostId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("post_type")]
        public string PostType { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        [Column("title")]
        public string Title { get; set; } = null!;

        [Required]
        [Column("content")]
        public string Content { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        [Column("author_type")]
        public string AuthorType { get; set; } = null!;

        [Column("author_id")]
        public int AuthorId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}
