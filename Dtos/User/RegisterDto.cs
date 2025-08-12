using System.ComponentModel.DataAnnotations;

namespace Dtos.User;

/// <summary>
/// Thông tin đăng ký tài khoản người dùng.
/// </summary>
public record RegisterDto
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
    [Required]
    public int EmployeeId { get; set; }
    //public string RoleId { get; set; } = "5d68e094-aeef-4eda-8bac-6d3ae58b3016";//viewer
}