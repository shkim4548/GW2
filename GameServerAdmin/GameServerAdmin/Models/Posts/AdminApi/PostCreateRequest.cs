using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Posts.AdminApi;

public class PostCreateRequest
{
    [Required]
    [MaxLength(20)]
    public string PostType { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    public string Content { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string AuthorType { get; set; } = null!;

    [Required]
    public int AuthorId { get; set; }
}

