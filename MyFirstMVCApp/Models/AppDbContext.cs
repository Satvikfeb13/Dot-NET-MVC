using Microsoft.EntityFrameworkCore;

namespace MyFirstMVCApp.Models
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
      
        public DbSet<User> Users { get; set; }

        public DbSet<Employee> Employees { get; set; }

       
    }
}
