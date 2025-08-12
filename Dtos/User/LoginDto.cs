using System.ComponentModel.DataAnnotations;

namespace Dtos.User;

/// <summary>
/// Tài khoản đăng nhập hệ thống
/// </summary>
public record LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}