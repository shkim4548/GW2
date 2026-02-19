using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Posts.PublicApi;

public sealed class PublicPostUpdateRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = null!;

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Content { get; set; } = null!;
}
