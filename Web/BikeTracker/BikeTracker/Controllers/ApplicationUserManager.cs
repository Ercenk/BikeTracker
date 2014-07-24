namespace WebApplication2.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;

    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.Owin;
    using Microsoft.Owin;
    using Microsoft.Owin.Security;

    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public async Task<IUser> GetClaimsAsync(string id)
        {
            throw new NotImplementedException();
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext arg2)
        {
            var manager = new ApplicationUserManager(new ApplicationUserStore());
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };
            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }


    public class ApplicationUserStore : IUserStore<ApplicationUser>, IUserLoginStore<ApplicationUser>
    {
        public void Dispose()
        {

        }

        public async Task CreateAsync(ApplicationUser user)
        {
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
        }

        public async Task DeleteAsync(ApplicationUser user)
        {
        }

        public async Task<ApplicationUser> FindByIdAsync(string userId)
        {
            return await
                Task.FromResult(
                    userId == "ercenk@hotmail.com"
                        ? new ApplicationUser { Id = "ercenk", UserName = "ercenk" }
                        : default(ApplicationUser));
        }

        public async Task<ApplicationUser> FindByNameAsync(string userName)
        {
            return await
                Task.FromResult(
                    userName == "ercenk@hotmail.com"
                        ? new ApplicationUser { Id = "ercenk", UserName = "ercenk" }
                        : default(ApplicationUser));
        }

        public Task AddLoginAsync(ApplicationUser user, UserLoginInfo login)
        {
            throw new NotImplementedException();
        }

        public Task RemoveLoginAsync(ApplicationUser user, UserLoginInfo login)
        {
            throw new NotImplementedException();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user)
        {
            throw new NotImplementedException();
        }

        public async Task<ApplicationUser> FindAsync(UserLoginInfo login)
        {
            var adminKey = BikeTrackerConfiguration.AdminKey;

            if (login.ProviderKey == adminKey)
            {
                return
                    await
                    Task.FromResult(new ApplicationUser { Id = "ercenk@ercenk.com", UserName = "ercenk@ercenk.com" });
            }

            return null;
        }
    }

    public class BikeTrackerConfiguration
    {
        public static string AdminKey
        {
            get
            {
                return "10152263092248564";
            }

            private set
            {
            }
        }
    }
}