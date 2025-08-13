using Microsoft.AspNetCore.Identity;

namespace EfCoreServices.Entities;

public class AppUser : IdentityUser
{
    public int EmployeeId { get; set; }

    public virtual Employee? Employee { get; set; } // Navigation property to Employee entity
}