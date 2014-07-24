// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Startup.cs" company="">
//   
// </copyright>
// <summary>
//   The startup.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace BikeTracker
{
    using System;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.Owin;
    using Microsoft.Owin;
    using Microsoft.Owin.Security.Cookies;
    using Microsoft.Owin.Security.Facebook;
    using Microsoft.Owin.Security.OAuth;

    using Owin;

    using WebApplication2.Controllers;

    /// <summary>
    /// The startup.
    /// </summary>
    public class Startup
    {
        #region Public Methods and Operators

        /// <summary>
        /// The configuration.
        /// </summary>
        /// <param name="app">
        /// The app.
        /// </param>
        public void Configuration(IAppBuilder app)
        {
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            app.UseCookieAuthentication(
                new CookieAuthenticationOptions
                    {
                        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                        LoginPath = new PathString("/Account/Login"),
                        Provider =
                            new CookieAuthenticationProvider
                                {
                                    OnValidateIdentity =
                                        SecurityStampValidator
                                        .OnValidateIdentity
                                        <ApplicationUserManager,
                                        ApplicationUser>(
                                            validateInterval:
                                        TimeSpan.FromMinutes(20),
                                            regenerateIdentity:
                                        (manager, user) =>
                                        user
                                            .GenerateUserIdentityAsync
                                            (manager))
                                }
                    });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(
                new OAuthAuthorizationServerOptions
                    {
                        TokenEndpointPath = new PathString("/Token"),
                        AuthorizeEndpointPath = new PathString("/Account/Authorize"),
                        Provider = new ApplicationOAuthProvider("web"),
                        AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(20),
                        AllowInsecureHttp = true
                    });

            app.UseFacebookAuthentication(appId: "773946142637304", appSecret: "e02d6d7a9319a707c9b2ce7fb121f955");


            #endregion
        }

        public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
        {
            private readonly string _publicClientId;

            public ApplicationOAuthProvider(string publicClientId)
            {
                if (publicClientId == null)
                {
                    throw new ArgumentNullException("publicClientId");
                }

                _publicClientId = publicClientId;
            }

            public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
            {
                if (context.ClientId == _publicClientId)
                {
                    Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                    if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                    {
                        context.Validated();
                    }
                    else if (context.ClientId == "web")
                    {
                        var expectedUri = new Uri(context.Request.Uri, "/");
                        context.Validated(expectedUri.AbsoluteUri);
                    }
                }

                return Task.FromResult<object>(null);
            }
        }
    }
}