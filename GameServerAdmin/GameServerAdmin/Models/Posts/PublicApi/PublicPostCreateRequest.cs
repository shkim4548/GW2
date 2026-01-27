using System.ComponentModel.DataAnnotations;

public class PublicPostCreateRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = null!;

    [Required]
    [StringLength(5000)]
    public string Content { get; set; } = null!;
}
