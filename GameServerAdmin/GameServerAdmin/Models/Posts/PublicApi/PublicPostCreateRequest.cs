using System.ComponentModel.DataAnnotations;

public class PublicPostCreateRequest
{
    /// <summary>
    /// 게시판 종류(카테고리) - 예: "General", "Free", "QnA"
    /// (추정) 현재 Domain이 string PostType을 사용하므로 유지.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PostType { get; set; } = "General";

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = null!;

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Content { get; set; } = null!;
}
