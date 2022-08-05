using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AspNetCore.Identity.CosmosDb;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mars.CreateDemoDatabase
{
    public class BuildDemoDb
    {
        private IConfigurationRoot _configuration;
        private readonly ResourceManagers _rm;

        public BuildDemoDb()
        {
            _rm = new ResourceManagers();
        }

        [FunctionName("BuildDemoDb")]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            using var dbContext = GetDbContext();

            await dbContext.Database.EnsureDeletedAsync();

            await dbContext.Database.EnsureCreatedAsync();

            await AddRoles(dbContext);

            await AddUsers(dbContext);
        }

        private async Task AddRoles(CosmosIdentityDbContext<IdentityUser, IdentityRole> dbContext)
        {
            var roleManager = _rm.GetRoleManager<IdentityRole>(dbContext);

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "User Administrators",
                NormalizedName = "User Administrators".ToUpperInvariant()
            });

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "User Administrators",
                NormalizedName = "User Administrators".ToUpperInvariant()
            });

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Human Resources",
                NormalizedName = "Human Resources".ToUpperInvariant()
            });

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Finance",
                NormalizedName = "Finance".ToUpperInvariant()
            });

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Operations",
                NormalizedName = "Operations".ToUpperInvariant()
            });

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Recruiting",
                NormalizedName = "Recruiting".ToUpperInvariant()
            });

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Editors",
                NormalizedName = "Editors".ToUpperInvariant()
            });

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Authors",
                NormalizedName = "Authors".ToUpperInvariant()
            });

            _ = await roleManager.CreateAsync(new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Reviewers",
                NormalizedName = "Reviewers".ToUpperInvariant()
            });
        }

        private async Task AddUsers(CosmosIdentityDbContext<IdentityUser, IdentityRole> dbContext)
        {
            var userManager = _rm.GetUserManager<IdentityUser>(dbContext);

            // Test Users
            var admin = await CreateUser(userManager, "admin@mason.com", "Star-Struck1");

            var adminUser = await userManager.FindByEmailAsync("admin@mason.com");
            adminUser.EmailConfirmed = true;
            await userManager.AddToRoleAsync(adminUser, "User Administrators");

            var perry = await CreateUser(userManager, "perry_mason@mason.com", $"A1a{Guid.NewGuid().ToString()}");
            var della = await CreateUser(userManager, "della_street@mason.com", $"A1a{Guid.NewGuid().ToString()}");
            var paul = await CreateUser(userManager, "paul_drake@mason.com", $"A1a{Guid.NewGuid().ToString()}");
            var aurthur = await CreateUser(userManager, "authur_tragg@mason.com", $"A1a{Guid.NewGuid().ToString()}");
            var hamilton = await CreateUser(userManager, "hamilton_burger@mason.com", $"A1a{Guid.NewGuid().ToString()}");
            var gertie = await CreateUser(userManager, "gertie_lade@mason.com", $"A1a{Guid.NewGuid().ToString()}");
        }

        private async Task<IdentityResult> CreateUser(UserManager<IdentityUser> userManager, string emailAddress, string password)
        {
            var user = new IdentityUser()
            {
                Email = emailAddress,
                NormalizedEmail = emailAddress.ToUpperInvariant(),
                UserName = emailAddress,
                NormalizedUserName = emailAddress.ToUpperInvariant(),
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            return result;
        }

        /// <summary>
        /// Get an instance of the Cosmos DB context.
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        public CosmosIdentityDbContext<IdentityUser, IdentityRole> GetDbContext(string connectionName = "ApplicationDbContextConnection")
        {
            var dbContext = new CosmosIdentityDbContext<IdentityUser, IdentityRole>(GetDbOptions(connectionName));
            return dbContext;
        }

        /// <summary>
        /// Get Cosmos DB Options
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        private DbContextOptions GetDbOptions(string connectionName = "ApplicationDbContextConnection")
        {
            var config = GetConfig();
            var connectionString = config.GetConnectionString("ApplicationDbContextConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = GetKeyValue("ApplicationDbContextConnection");
            }

            var dbName = GetKeyValue("CosmosIdentityDbName");


            var builder = new DbContextOptionsBuilder();
            builder.UseCosmos(connectionString, dbName);

            return builder.Options;
        }

        /// <summary>
        /// Gets the configuration
        /// </summary>
        /// <returns></returns>
        private IConfigurationRoot GetConfig()
        {
            if (_configuration != null) return _configuration;

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var jsonConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonFile(jsonConfig, true)
                .AddEnvironmentVariables() // Added to read environment variables from GitHub Actions
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true); // User secrets override all - put here

            _configuration = Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(1));

            return _configuration;
        }

        /// <summary>
        /// Gets the value of a configuration key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetKeyValue(string key)
        {
            return GetKeyValue(GetConfig(), key);
        }

        private string GetKeyValue(IConfigurationRoot config, string key)
        {
            var data = config[key];

            if (string.IsNullOrEmpty(data))
            {
                // First attempt to get the value of the key as named.
                data = Environment.GetEnvironmentVariable(key);

                if (string.IsNullOrEmpty(data))
                {
                    // For Github Actions, secrets are forced upper case
                    data = Environment.GetEnvironmentVariable(key.ToUpper());
                }
            }
            return string.IsNullOrEmpty(data) ? string.Empty : data;
        }
    }
}
