using Microsoft.EntityFrameworkCore;
using BackendAPI.Models;
using System.Text.Json;

namespace BackendAPI.Models
{
    public class APIDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Neighbourhood> Neighbourhoods { get; set; }
        public APIDbContext(DbContextOptions<APIDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().Property(e => e.Settings).HasConversion(s => JsonSerializer.Serialize(s, null), s => JsonSerializer.Deserialize<Settings>(s, null));
        }
    }
}