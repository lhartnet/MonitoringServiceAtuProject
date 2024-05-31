using Microsoft.EntityFrameworkCore;
using MonitoringService.Domain.Models;

namespace MonitoringService.Persistence
{
    /// <summary>
    /// A class to represent the database 
    /// </summary>
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
        }

        public DbSet<SpecDetails> SpecDetails { get; set; }
    }
}
