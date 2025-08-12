namespace Dtos.User;

/// <summary>
/// Role Data Transfer Object (DTO) for representing user roles in the system.
/// </summary>
public record RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}