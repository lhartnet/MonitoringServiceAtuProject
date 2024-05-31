using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
