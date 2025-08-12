namespace Dtos.User;

/// <summary>
/// UserIdRoleName represents a mapping between a user ID and a role name.
/// </summary>
public record UserIdRoleName
{
    public string UserId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}