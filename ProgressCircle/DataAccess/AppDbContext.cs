using Microsoft.EntityFrameworkCore;
using ProgressCircle.Models;

namespace ProgressCircle.DataAccess
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
             : base(options)
        {
        }

        public DbSet<Diagram> Diagrams { get; set; }
    }
}
