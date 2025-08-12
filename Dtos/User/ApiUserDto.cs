namespace Dtos.User;

/// <summary>
/// ApiUserDto represents a user in the system with their ID, username, roles, and associated employee ID.
/// </summary>
/// <param name="Id"></param>
/// <param name="UserName"></param>
/// <param name="Roles"></param>
/// <param name="EmployeeId"></param>
public record ApiUserDto(string Id, string UserName, string Roles, int EmployeeId);