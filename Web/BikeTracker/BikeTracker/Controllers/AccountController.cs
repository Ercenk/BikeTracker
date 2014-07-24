using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Owin;

namespace WebApplication2.Controllers
{
    using BikeTracker;

    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationUserManager _userManager;

        public AccountController()
        {
            
        }

        public AccountController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }


        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

       // The Authorize Action is the end point which gets called when you access any
        // protected Web API. If the user is not logged in then they will be redirected to 
        // the Login page. After a successful login you can call a Web API.
        [HttpGet]
        public ActionResult Authorize()
        {
            var claims = new ClaimsPrincipal(User).Claims.ToArray();
            var identity = new ClaimsIdentity(claims, "Bearer");
            return new EmptyResult();
        }

        ////
        //// GET: /Account/Login
        //[AllowAnonymous]
        //public async Task<ActionResult> Login(string returnUrl)
        //{
        //    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);

        //    var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
        //}

        [AllowAnonymous]
        public ActionResult Login()
        {
            return new ChallengeResult(Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = "/" }));
        }

        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var user = await UserManager.FindAsync(loginInfo.Login);
            if (user != null)
            {
                await SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }

            return null;
         }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, await user.GenerateUserIdentityAsync(UserManager));
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                // TODO: Dogy
                return RedirectToAction("Index", "Home");
            }
        }
        //private async Task SaveAccessToken(ApplicationUser user, ClaimsIdentity identity)
        //{
        //    var userclaims = await this.UserManager.GetClaimsAsync(user.Id);

        //    foreach (var at in (
        //        from claims in identity.Claims
        //        where claims.Type.EndsWith("access_token")
        //        select new Claim(claims.Type, claims.Value, claims.ValueType, claims.Issuer)))
        //    {

        //        if (!userclaims.Contains(at))
        //        {
        //            await UserManager.AddClaimAsync(user.Id, at);
        //        }
        //    }
        //}

        //private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        //{
            
            

        //    await SetExternalProperties(identity);

        //    AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);

        //    await SaveAccessToken(user, identity);
        //}

        //private async Task SetExternalProperties(ClaimsIdentity identity)
        //{
        //    // get external claims captured in Startup.ConfigureAuth
        //    ClaimsIdentity ext = await AuthenticationManager.GetExternalIdentityAsync(DefaultAuthenticationTypes.ExternalCookie);

        //    if (ext != null)
        //    {
        //        var ignoreClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims";
        //        // add external claims to identity
        //        foreach (var c in ext.Claims)
        //        {
        //            if (!c.Type.StartsWith(ignoreClaim))
        //                if (!identity.HasClaim(c.Type, c.Value))
        //                    identity.AddClaim(c);
        //        }
        //    }
        //}
    }

    public class ChallengeResult : HttpUnauthorizedResult
    {
        public ChallengeResult(string redirectUri)
            : this(redirectUri, null)
        {
        }

        public ChallengeResult(string redirectUri, string userId)
        {
            LoginProvider = "Facebook";
            RedirectUri = redirectUri;
            UserId = userId;
        }

        public string LoginProvider { get; set; }
        public string RedirectUri { get; set; }
        public string UserId { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
            if (UserId != null)
            {
                properties.Dictionary["XsrfId"] = UserId;
            }
            context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
        }
    }
}