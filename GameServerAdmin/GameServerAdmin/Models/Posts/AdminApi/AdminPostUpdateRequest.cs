using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Posts.AdminApi;

public class AdminPostUpdateRequest
{
    [Required]
    public int PostId { get; set; }

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Title { get; set; } = null!;
    
    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Content { get; set; } = null!;
}
