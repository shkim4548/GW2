using System.ComponentModel.DataAnnotations;
using GameServerAdmin.Domain.Notices;

namespace GameServerAdmin.Models.Notices.AdminApi
{
    public class CreateNoticeRequest
    {
        [Required(ErrorMessage = "제목은 필수입니다.")]
        [StringLength(200, ErrorMessage = "제목은 200자 이하로 입력해주세요.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "내용은 필수입니다.")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public NoticeCategory Category { get; set; }

        [Required]
        public NoticePriority Priority { get; set; } = NoticePriority.Normal;

        public DateTime? DisplayStartAt { get; set; }
        public DateTime? DisplayEndAt { get; set; }
    }
}