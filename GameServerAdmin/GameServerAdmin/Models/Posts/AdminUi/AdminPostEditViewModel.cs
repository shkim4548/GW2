using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Posts.AdminUi
{
    public sealed class AdminPostEditViewModel
    {
        public long PostId { get; init; } // Hidden 필드/Route용

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;
    }
}
