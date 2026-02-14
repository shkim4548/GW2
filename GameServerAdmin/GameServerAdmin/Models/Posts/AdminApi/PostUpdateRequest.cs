using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Posts.AdminApi;

public class PostUpdateRequest
{
    [Required]
    public int PostId { get; set; }
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;
    [Required]
    public string Content { get; set; } = null!;
}
