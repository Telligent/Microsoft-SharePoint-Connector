using Microsoft.SharePoint.Client;
using System;
using System.Security.Principal;
using System.Web;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.SecurityModules;
using SPLog = Telligent.Evolution.Extensions.SharePoint.Components.Data.Log.SPLog;

namespace Telligent.Evolution.Extensions.SharePoint.Components
{
    public class SPContext : ClientContext
    {
        private const string TokenKey = "FedAuth";

        private bool? _isServiceAccount;
        private string _accessToken;
        private string _samlToken;

        private readonly string siteUrl;
        private readonly Authentication authentication;
        private readonly WindowsImpersonationContext impersonate;

        public SPContext(Uri webFullUrl, Authentication authentication, bool runAsServiceAccount = false)
            : this(webFullUrl.AbsoluteUri, authentication, runAsServiceAccount) { }

        public SPContext(string webFullUrl, Authentication authentication, bool runAsServiceAccount = false)
            : base(webFullUrl)
        {
            if (runAsServiceAccount)
            {
                IsServiceAccount = true;
            }

            siteUrl = webFullUrl;
            ExecutingWebRequest += ClientContextExecutingWebRequest;

            this.authentication = authentication;

            if (this.authentication is ServiceAccount)
            {
                Credentials = this.authentication.Credentials();
            }
            else if (this.authentication is AuthenticationUtil.Methods.OAuth && IsServiceAccount)
            {
                Credentials = this.authentication.Credentials();
            }
            else if (this.authentication is Windows)
            {
                impersonate = Impersonate();
            }
        }

        private bool IsServiceAccount
        {
            get
            {
                if (_isServiceAccount == null)
                {
                    var hasAccessToken = !string.IsNullOrEmpty(OAuthToken);
                    if (hasAccessToken)
                    {
                        _isServiceAccount = false;
                    }
                    else
                    {
                        var accessingUser = Extensibility.Api.Version1.PublicApi.Users.AccessingUser;
                        var isSystemAcccount = accessingUser == null || (accessingUser.IsSystemAccount.HasValue && accessingUser.IsSystemAccount.Value);
                        Func<bool> isAdminWithoutToken = () => accessingUser != null && Extensibility.Api.Version1.PublicApi.RoleUsers.IsUserInRoles(accessingUser.Username, new[] { "Administrators" });
                        _isServiceAccount = isSystemAcccount || isAdminWithoutToken();
                    }
                }
                return _isServiceAccount.Value;
            }
            set
            {
                _isServiceAccount = value;
            }
        }

        public string OAuthToken
        {
            get
            {
                if (string.IsNullOrEmpty(_accessToken) && HttpContext.Current != null)
                {
                    var tokensCookie = HttpContext.Current.Request.Cookies.Get(TokenKey);
                    if (tokensCookie == null || string.IsNullOrEmpty(tokensCookie.Value))
                    {
                        tokensCookie = HttpContext.Current.Response.Cookies.Get("AuthorizationOffice");
                        if (tokensCookie == null || string.IsNullOrEmpty(tokensCookie.Value))
                        {
                            return null;
                        }
                    }
                    _accessToken = tokensCookie.Value;
                }
                return _accessToken;
            }
        }

        public string SAMLToken
        {
            get
            {
                if (string.IsNullOrEmpty(_samlToken))
                {
                    _accessToken = SAML.GetToken(siteUrl);
                }
                return _accessToken;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (impersonate != null)
                impersonate.Undo();
        }

        private WindowsImpersonationContext Impersonate()
        {
            try
            {
                var context = CSContext.Current.Context;
                if (context == null) return null;

                var ssop = (SingleSignOnPrincipal)context.User;
                var principal = (WindowsPrincipal)ssop.OriginalPrincipal;
                var id = (WindowsIdentity)principal.Identity;

                return id.Impersonate();
            }
            catch (Exception ex)
            {
                SPLog.UserInvalidCredentials(ex, string.Format("Enable Windows Authentication and set roleManager enabled=\"false\" : {0} {1}", ex.Message, ex.StackTrace));
            }

            return null;
        }

        private void ClientContextExecutingWebRequest(object sender, WebRequestEventArgs e)
        {
            if (authentication is AuthenticationUtil.Methods.OAuth
                && !IsServiceAccount
                && !string.IsNullOrEmpty(OAuthToken))
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + OAuthToken;
                return;
            }

            e.WebRequestExecutor.WebRequest.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");

            if (authentication is SAML)
            {
                // Update the web request used by the sharepoint managed client to include the FedAuth cookie
                e.WebRequestExecutor.WebRequest.Headers.Add("Cookie", string.Format("FedAuth={0}", SAMLToken));
            }
        }
    }
}