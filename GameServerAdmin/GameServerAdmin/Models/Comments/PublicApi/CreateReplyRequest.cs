using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Comments.PublicApi
{
    public class CreateReplyRequest
    {
        [Required(ErrorMessage = "답글 내용은 필수입니다")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "답글은 1자 이상 1000자 이하로 작성해주세요.")]
        public string Content { get; set; } = string.Empty;
    }
}
