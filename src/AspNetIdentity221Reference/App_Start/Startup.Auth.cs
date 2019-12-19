using System;
using System.Configuration;
using Owin;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using AspNetIdentity221Reference.Models;
using System.Threading.Tasks;
using System.Security.Claims;
//using System.Configuration;
//using System.Security.Claims;
//using System.Threading.Tasks;

namespace AspNetIdentity221Reference
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app) {
            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ExternalCookie);
            app.UseCookieAuthentication(new CookieAuthenticationOptions {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                //AuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                //AuthenticationMode = AuthenticationMode.Passive,
                //CookieName = ".AspNet." + DefaultAuthenticationTypes.ExternalCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    //OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser, int>(
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30), //  5
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager) //)
                        //regenerateIdentityCallback: (manager, user) => user.GenerateUserIdentityAsync(manager),
                        //getUserIdCallback: (id) => id.GetUserId<int>()
                    )
                }
            });
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Enables the application to remember the second login verification factor such as phone or email.
            // Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from.
            // This is similar to the RememberMe option when you log in.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions() {
            //    ClientId = ConfigurationManager.AppSettings["GoogleClientId"],
            //    ClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"]
            //});

            var googleAuthenticationOptions = new GoogleOAuth2AuthenticationOptions() {
                ClientId = ConfigurationManager.AppSettings["GoogleAuthenticationId"],
                ClientSecret = ConfigurationManager.AppSettings["GoogleAuthenticationSecret"]
                // https://github.com/aspnet/AspNetKatana/issues/251#issuecomment-449587635
                // https://github.com/aspnet/AspNetCore/issues/6069
                , UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo",
                BackchannelHttpHandler = new GoogleUserInfoRemapper(new System.Net.Http.WebRequestHandler())                
            };
            //googleAuthenticationOptions.Scope.Add("profile");
            googleAuthenticationOptions.Scope.Add("email");

            googleAuthenticationOptions.Provider = new GoogleOAuth2AuthenticationProvider() {
                OnAuthenticated = context => {
                    //var profileUrl = context.User["image"]["url"].ToString();
                    ////var email = context.User["email"]["url"].ToString();
                    //context.Identity.AddClaim(new Claim(ClaimTypes.Uri, profileUrl, ClaimValueTypes.String, "Google"));
                    ////context.Identity.AddClaim(new Claim(ClaimTypes.Email, profileUrl, ClaimValueTypes.String, "Google"));
                    context.Identity.AddClaim(new Claim(ClaimTypes.Email, context.Identity.FindFirst(ClaimTypes.Email).Value));
                    return Task.FromResult(0);
                }
            };

            app.UseGoogleAuthentication(googleAuthenticationOptions);
        }

        // https://github.com/aspnet/AspNetKatana/issues/251#issuecomment-449587635
        internal class GoogleUserInfoRemapper : System.Net.Http.DelegatingHandler
        {
            public GoogleUserInfoRemapper(System.Net.Http.HttpMessageHandler innerHandler) : base(innerHandler) { }

            protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) {
                var response = await base.SendAsync(request, cancellationToken);

                if (!request.RequestUri.AbsoluteUri.Equals("https://www.googleapis.com/oauth2/v2/userinfo")) {
                    return response;
                }

                response.EnsureSuccessStatusCode();
                var text = await response.Content.ReadAsStringAsync();
                Newtonsoft.Json.Linq.JObject user = Newtonsoft.Json.Linq.JObject.Parse(text);
                Newtonsoft.Json.Linq.JObject legacyFormat = new Newtonsoft.Json.Linq.JObject();

                Newtonsoft.Json.Linq.JToken token;
                if (user.TryGetValue("id", out token)) {
                    legacyFormat["id"] = token;
                }
                if (user.TryGetValue("name", out token)) {
                    legacyFormat["displayName"] = token;
                }
                Newtonsoft.Json.Linq.JToken given, family;
                if (user.TryGetValue("given_name", out given) && user.TryGetValue("family_name", out family)) {
                    var name = new Newtonsoft.Json.Linq.JObject();
                    name["givenName"] = given;
                    name["familyName"] = family;
                    legacyFormat["name"] = name;
                }
                if (user.TryGetValue("link", out token)) {
                    legacyFormat["url"] = token;
                }
                if (user.TryGetValue("email", out token)) {
                    var email = new Newtonsoft.Json.Linq.JObject();
                    email["value"] = token;
                    legacyFormat["emails"] = new Newtonsoft.Json.Linq.JArray(email);
                }
                if (user.TryGetValue("picture", out token)) {
                    var image = new Newtonsoft.Json.Linq.JObject();
                    image["url"] = token;
                    legacyFormat["image"] = image;
                }

                text = legacyFormat.ToString();
                response.Content = new System.Net.Http.StringContent(text);
                return response;
            }
        }
    }
}