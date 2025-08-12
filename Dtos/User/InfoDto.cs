namespace Dtos.User;
/// <summary>
/// DTO đại diện cho thông tin người dùng.
/// </summary>
public record InfoDto
{
    /// <summary>
    /// ID của người dùng kiểu Guid (là mã định danh duy nhất).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Email của người dùng.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Tên đăng nhập của người dùng.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách tên vai trò của người dùng.
    /// </summary>
    public IEnumerable<string>? RoleNames { get; set; }
}