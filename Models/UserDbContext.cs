using Microsoft.EntityFrameworkCore;
using BackendAPI.Models;
using System.Text.Json;

namespace BackendAPI.Models
{
    class UserDbContext : DbContext
    {
        public DbSet<User> Users {get; set;}
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("Server=localhost;Database=CovidAlerter;Uid=root;Pwd=mahan1387;");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().Property(e => e.Settings).HasConversion(s => JsonSerializer.Serialize(s, null), s => JsonSerializer.Deserialize<Settings>(s, null));
        }
    }
}