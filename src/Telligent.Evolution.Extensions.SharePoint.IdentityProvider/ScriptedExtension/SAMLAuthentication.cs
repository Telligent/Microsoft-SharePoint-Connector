using Microsoft.IdentityModel.Protocols.WSFederation;
using Microsoft.IdentityModel.SecurityTokenService;
using Microsoft.IdentityModel.Web;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Telligent.Evolution.Extensions.SharePoint.IdentityProvider.STS;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider.ScriptedExtension
{
    public class SAMLAuthentication : ISAMLAuthentication
    {
        static readonly object SyncRoot = new object();
        private STSConfiguration telligentConfiguration;

        /// <summary>
        /// Provides a model for creating a single Configuration object for the application. The first call creates a new CustomSecruityTokenServiceConfiguration and 
        /// Subsequent calls will return the same Configuration object. This maintains any state that is set between calls and improves performance.
        /// </summary>
        public STSConfiguration Configuration
        {
            get
            {
                if (telligentConfiguration == null)
                {
                    lock (SyncRoot)
                    {
                        var plugin = IdentityProviderPlugin.Plugin;
                        if (plugin != null)
                        {
                            var issuerName = plugin.Configuration.GetString(IdentityProviderPlugin.PropertyId.IssuerName);
                            var certificateName = plugin.Configuration.GetString(IdentityProviderPlugin.PropertyId.SigningCertificateName);
                            var cred = new X509SigningCredentials(CertificateUtil.GetCertificate(StoreName.My, StoreLocation.LocalMachine, certificateName));

                            telligentConfiguration = new STSConfiguration(issuerName, cred);
                        }
                    }
                }
                return telligentConfiguration;
            }
        }

        public bool LoginAsAnotherUser(string returnUrl)
        {
            var res = false;
            Uri url;

            if (Uri.TryCreate(returnUrl, UriKind.RelativeOrAbsolute, out url))
            {
                var queryString = HttpUtility.ParseQueryString(url.Query);
                if (queryString.AllKeys.Contains("loginasanotheruser"))
                {
                    bool.TryParse(queryString["loginasanotheruser"], out res);
                }
            }

            if (res)
            {
                return !HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query).AllKeys.Contains("post");
            }

            return false;
        }

        /// <summary>
        ///  Returns URL for redirect after User Login
        /// </summary>
        /// <returns></returns>
        public string Url()
        {
            var uri = HttpContext.Current.Request.Url;

            // remove [loginasanotheruser] from wctx query string
            var wctx = HttpContext.Current.Request.QueryString["wctx"];
            var wctxUri = new Uri(wctx);
            var wctxQueryString = HttpUtility.ParseQueryString(wctxUri.Query);
            var newWctxUri = string.Concat(
                wctxUri.GetLeftPart(UriPartial.Path),
                "?",
                String.Join("&",
                    wctxQueryString.AllKeys
                        .Where(key => !string.Equals(key, "loginasanotheruser", StringComparison.InvariantCultureIgnoreCase))
                        .Select(key => string.Concat(key, "=", HttpUtility.UrlEncode(wctxQueryString[key])))
                    )
                );

            // update wctx key in the current uri
            var uriQueryString = HttpUtility.ParseQueryString(uri.Query);
            return string.Concat(
                uri.GetLeftPart(UriPartial.Path),
                "?",
                String.Join("&",
                    uriQueryString.AllKeys
                        .Select(key => string.Concat(key, "=", HttpUtility.UrlEncode(key == "wctx" ? newWctxUri : uriQueryString[key])))
                    )
                );
        }

        public bool SignIn()
        {
            try
            {
                SecurityTokenService sts = new TelligentSTS(Configuration);

                var requestMessage = WSFederationMessage.CreateFromUri(HttpContext.Current.Request.Url) as SignInRequestMessage;
                var responseMessage = FederatedPassiveSecurityTokenServiceOperations.ProcessSignInRequest(requestMessage, HttpContext.Current.User, sts);

                FederatedPassiveSecurityTokenServiceOperations.ProcessSignInResponse(responseMessage, HttpContext.Current.Response);
            }
            catch (Exception)
            {
                SPLog.Event("SAML Authentication SignIn failed or FedAuth cookie expired.");
            }

            return true;
        }

        public void SignOut()
        {
            try
            {
                var requestMessage = WSFederationMessage.CreateFromUri(HttpContext.Current.Request.Url) as SignOutRequestMessage;
                if (requestMessage == null) return;

                FederatedPassiveSecurityTokenServiceOperations.ProcessSignOutRequest(requestMessage, HttpContext.Current.User, requestMessage.Reply, HttpContext.Current.Response);
            }
            catch (Exception)
            {
                SPLog.Event("SAML Authentication SignOut failed or FedAuth cookie expired.");
            }
        }
    }
}
