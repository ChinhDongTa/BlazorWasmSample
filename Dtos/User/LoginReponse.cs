namespace Dtos.User;

/// <summary>
/// Thông tin phản hồi đăng nhập hệ thống
/// </summary>
public record LoginReponse
{
    public string TokenType { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    //public DateTime ExpireAt => DateTime.UtcNow.AddSeconds(ExpiresIn);
}