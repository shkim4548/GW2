using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Admin.AdminUi;

public sealed class CreateAdminViewModel
{
    [Required(ErrorMessage = "아이디를 입력하세요.")]
    [MaxLength(50)]
    [Display(Name = "아이디")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호를 입력하세요.")]
    [DataType(DataType.Password)]
    [Display(Name = "비밀번호")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호 확인을 입력하세요.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "비밀번호가 일치하지 않습니다.")]
    [Display(Name = "비밀번호 확인")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
