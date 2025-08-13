using EfCoreServices.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EfCoreServices.Data;

//(DbContextOptions<AppDbContext> options)
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Employee> Employees { get; set; } = null!; // DbSet cho Employee entity

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    if (!optionsBuilder.IsConfigured)
    //    {
    //        optionsBuilder.UseSqlServer("Data Source=10.52.240.22;Initial Catalog=DongTaSampleDb;User ID=sa;Password=123456a@; Trust Server Certificate=True;");
    //    }
    //}
}