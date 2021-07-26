using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Models
{
    class UserDbContext : DbContext
    {
        public DbSet<User> Users {get; set;}
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("Server=localhost;Database=CovidAlerter;Uid=root;Pwd=mahan1387;");
        }
    }
}