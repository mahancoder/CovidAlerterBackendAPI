using Microsoft.EntityFrameworkCore;
using BackendAPI.Models;
using System.Text.Json;

namespace BackendAPI.Models
{
    public class APIDbContext : DbContext
    {
        public DbSet<User> Users {get; set;}
        public DbSet<Report> Reports { get; set; }
        public APIDbContext(DbContextOptions<APIDbContext> options) : base(options)
        {
        }
    }
}