using AspNetCore.Identity.CosmosDb;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cosmos.IdentityManagement.Website.Data
{
    public class ApplicationDbContext : CosmosIdentityDbContext<IdentityUser, IdentityRole>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<CimSetting>().HasPartitionKey("Id").UseETagConcurrency().ToContainer("CimSettings");

            base.OnModelCreating(builder);
        }

        /// <summary>
        /// Holds settings specific to this installation.
        /// </summary>
        DbSet<CimSetting> CimSettings { get; set; }
    }
}