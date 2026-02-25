using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Auth
{
    public class RegisterRequest
    {
        [Required]
        [Display(Name = "아이디")]
        public string UserName { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "비밀번호")]
        public string Password { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "비밀번호 확인")]
        [Compare("Password", ErrorMessage = "비밀번호가 일치하지 않습니다.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}