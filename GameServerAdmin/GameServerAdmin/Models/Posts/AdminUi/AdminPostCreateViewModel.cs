using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Posts.AdminUi;

public sealed class AdminPostCreateViewModel
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    [Display(Name = "게시글 유형")]
    public string PostType { get; set; } = "General";

    [Required]
    [StringLength(200, MinimumLength = 1)]
    [Display(Name = "제목")]
    public string Title { get; set; } = null!;

    [Required]
    [Display(Name = "내용")]
    public string Content { get; set; } = null!;
}
