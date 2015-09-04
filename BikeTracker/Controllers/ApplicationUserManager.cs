namespace WebApplication2.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI.WebControls;

    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.Owin;
    using Microsoft.Owin;
    using Microsoft.Owin.Security;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Table;

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
                RequiredLength = 0,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
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
        private CloudTable userTable;

        public const string UsersTableName = "users";

        public ApplicationUserStore()
        {
            this.userTable =
                new CloudStorageAccount(
                    new StorageCredentials(
                        CloudConfigurationManager.GetSetting("storageAccountName"),
                        CloudConfigurationManager.GetSetting("storageAccountKey")),
                    true).CreateCloudTableClient().GetTableReference(UsersTableName);

            this.userTable.CreateIfNotExists();
        }
        public void Dispose()
        {

        }

        public async Task CreateAsync(ApplicationUser user)
        {
            var userData = new UserData(user);
            var insertOperation = TableOperation.Insert(userData);
            await this.userTable.ExecuteAsync(insertOperation);
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
            var userData = new UserData(user);
            var updateOperation = TableOperation.Replace(userData);
            await this.userTable.ExecuteAsync(updateOperation);
        }

        public async Task DeleteAsync(ApplicationUser user)
        {
            var userData = new UserData(user);
            var deleteOperation = TableOperation.Delete(userData);
            await this.userTable.ExecuteAsync(deleteOperation);
        }

        public async Task<ApplicationUser> FindByIdAsync(string userId)
        {
            var retrieveOperation = TableOperation.Retrieve<UserData>(UserData.PartitionKeyValue, userId);
            var result = await this.userTable.ExecuteAsync(retrieveOperation);

            if (result == null || result.Result == null)
            {
                return null;
            }

            var userData = (UserData)result.Result;

            return userData.GetApplicationUser();
        }

        public async Task<ApplicationUser> FindByNameAsync(string userName)
        {
            var query =
                new TableQuery<UserData>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(
                            "PartitionKey",
                            QueryComparisons.Equal,
                            UserData.PartitionKeyValue),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("UserName", QueryComparisons.Equal, userName)));

            TableQuerySegment<UserData> segment = null;
            var queryResult = new List<UserData>();
            while (segment == null || segment.ContinuationToken != null)
            {
                segment = await this.userTable.ExecuteQuerySegmentedAsync(query, segment.ContinuationToken);
                queryResult.AddRange(segment.Results);
            }

            return !queryResult.Any() ? null : queryResult.First().GetApplicationUser();
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

    public class UserData : TableEntity
    {
        public const string PartitionKeyValue = "user";

        public UserData()
        {
            
        }

        public UserData(ApplicationUser user)
        {
            this.PartitionKey = PartitionKeyValue;
            this.RowKey = user.Id;
            this.Id = user.Id;
            this.UserName = user.UserName;
            this.Role = user.Role;
            this.Email = user.Email;
            this.FirstName = user.FirstName;
            this.LastName = user.LastName;
        }

        public string LastName { get; set; }

        public string FirstName { get; set; }

        public string Id { get; set; }

        public string UserName { get; set; }

        public string Role { get; set; }

        public string Email { get; set; }

        public ApplicationUser GetApplicationUser()
        {
            return new ApplicationUser { Email = this.Email, Id = this.Id, UserName = this.UserName, Role = this.Role };
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