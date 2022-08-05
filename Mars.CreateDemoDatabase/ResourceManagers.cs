using AspNetCore.Identity.CosmosDb;
using AspNetCore.Identity.CosmosDb.Repositories;
using AspNetCore.Identity.CosmosDb.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mars.CreateDemoDatabase
{
    /// <summary>
    /// User and role managers
    /// </summary>
    public class ResourceManagers
    {
        /// <summary>
        /// Gets a user manager for testing purposes
        /// </summary>
        /// <typeparam name="TUser"></typeparam>
        /// <param name="store"></param>
        /// <returns></returns>
        public UserManager<IdentityUser> GetUserManager<TUser>(CosmosIdentityDbContext<IdentityUser, IdentityRole> dbContext) where TUser : class
        {
            var builder = new IdentityBuilder(typeof(IdentityUser), new ServiceCollection());

            var userType = builder.UserType;

            var dataProtectionProviderType = typeof(DataProtectorTokenProvider<>).MakeGenericType(userType);
            var phoneNumberProviderType = typeof(PhoneNumberTokenProvider<>).MakeGenericType(userType);
            var emailTokenProviderType = typeof(EmailTokenProvider<>).MakeGenericType(userType);
            var authenticatorProviderType = typeof(AuthenticatorTokenProvider<>).MakeGenericType(userType);
            //var authenticatorProviderType = typeof(UserTwoFactorTokenProvider<>).MakeGenericType(userType);

            var store = GetUserStore(dbContext);

            var options = new Mock<IOptions<IdentityOptions>>();
            var idOptions = new IdentityOptions();

            options.Setup(o => o.Value).Returns(idOptions);
            var userValidators = new List<IUserValidator<IdentityUser>>();
            var validator = new Mock<IUserValidator<IdentityUser>>();
            userValidators.Add(validator.Object);
            var pwdValidators = new List<PasswordValidator<IdentityUser>>();
            pwdValidators.Add(new PasswordValidator<IdentityUser>());
            var userManager = new UserManager<IdentityUser>(store, options.Object, new PasswordHasher<IdentityUser>(),
                userValidators, pwdValidators, MockLookupNormalizer(),
                new IdentityErrorDescriber(), null,
                new Mock<ILogger<UserManager<IdentityUser>>>().Object);
            validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<IdentityUser>()))
                .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();

            return userManager;
        }
        
        /// <summary>
        /// Gets a role manager
        /// </summary>
        /// <typeparam name="TRole"></typeparam>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public RoleManager<IdentityRole> GetRoleManager<TRole>(CosmosIdentityDbContext<IdentityUser, IdentityRole> dbContext) where TRole : class
        {
            var store = GetRoleStore(dbContext);
            var roles = new List<IRoleValidator<IdentityRole>>();
            roles.Add(new RoleValidator<IdentityRole>());
            var roleManager = new RoleManager<IdentityRole>(store, roles, MockLookupNormalizer(),
                new IdentityErrorDescriber(), new Mock<ILogger<RoleManager<IdentityRole>>>().Object);
            return roleManager;
        }

        private ILookupNormalizer MockLookupNormalizer()
        {
            var normalizerFunc = new Func<string, string>(i =>
            {
                if (i == null)
                {
                    return null;
                }
                else
                {
                    return i.ToUpperInvariant();
                }
            });
            var lookupNormalizer = new Mock<ILookupNormalizer>();
            lookupNormalizer.Setup(i => i.NormalizeName(It.IsAny<string>())).Returns(normalizerFunc);
            lookupNormalizer.Setup(i => i.NormalizeEmail(It.IsAny<string>())).Returns(normalizerFunc);
            return lookupNormalizer.Object;
        }

        /// <summary>
        /// Get an instance of the Cosmos DB user store.
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        private CosmosUserStore<IdentityUser> GetUserStore(CosmosIdentityDbContext<IdentityUser, IdentityRole> dbContext)
        {
            var repository = new CosmosIdentityRepository<CosmosIdentityDbContext<IdentityUser, IdentityRole>, IdentityUser, IdentityRole>(dbContext);

            var userStore = new CosmosUserStore<IdentityUser>(repository);

            return userStore;
        }

        /// <summary>
        /// Get an instance of the Cosmos DB role store
        /// </summary>
        /// <returns></returns>
        private CosmosRoleStore<IdentityUser, IdentityRole> GetRoleStore(CosmosIdentityDbContext<IdentityUser, IdentityRole> dbContext)
        {
            var repository = new CosmosIdentityRepository<CosmosIdentityDbContext<IdentityUser, IdentityRole>, IdentityUser, IdentityRole>(dbContext);

            var rolestore = new CosmosRoleStore<IdentityUser, IdentityRole>(repository);

            return rolestore;
        }

    }

}
