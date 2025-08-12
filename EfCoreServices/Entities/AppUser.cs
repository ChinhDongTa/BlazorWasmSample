using Microsoft.AspNetCore.Identity;

namespace EfCoreServices.Entities;

public class AppUser : IdentityUser
{
    public int EmployeeId { get; set; }
}