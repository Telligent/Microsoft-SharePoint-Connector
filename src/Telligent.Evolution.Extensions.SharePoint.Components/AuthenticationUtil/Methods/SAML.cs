using System;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.UI;
using Microsoft.IdentityModel.Protocols.WSFederation;
using Microsoft.IdentityModel.SecurityTokenService;
using Microsoft.IdentityModel.Web;
using Telligent.Evolution.Extensions.SharePoint.IdentityProvider;
using Telligent.Evolution.Extensions.SharePoint.IdentityProvider.ScriptedExtension;
using Telligent.Evolution.Extensions.SharePoint.IdentityProvider.STS;

namespace Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods
{
    public class SAML : Authentication
    {
        public override string Name
        {
            get { return "SAML"; }
        }

        public override string Text
        {
            get { return "SAML Authentication"; }
        }

        public SAML() { }
        public SAML(string query) { }

        public override ICredentials Credentials()
        {
            return null;
        }

        public override string ToQueryString()
        {
            return String.Format("{0}={1}", AuthKey, Name);
        }

        public override void CreateMarkup(Control container) { }

        public override Authentication ParseMarkup(Control markup)
        {
            return new SAML();
        }

        /// <summary>
        /// Get SAML Token using SharePoint STS
        /// </summary>
        /// <param name="spSiteUrl">SharePoint site collection url</param>
        /// <returns>SAML Token</returns>
        public static string GetToken(string spSiteUrl)
        {
            try
            {
                Uri spsite;
                if (!Uri.TryCreate(spSiteUrl, UriKind.Absolute, out spsite))
                {
                    // unable to connect to SharePoint and get token
                    return String.Empty;
                }

                string sharePointBaseUrl = spsite.GetLeftPart(UriPartial.Authority);
                var sharePointInfo = new
                {
                    Wctx = sharePointBaseUrl + "/_layouts/Authenticate.aspx?Source=%2F",
                    Wtrealm = sharePointBaseUrl + "/_trust"
                };

                // generate SAML to post on SharePoint STS
                var stsResponse = STSResponse(sharePointBaseUrl, sharePointInfo.Wtrealm);
                var sharepointRequest = WebRequest.Create(sharePointInfo.Wtrealm) as HttpWebRequest;
                if (sharepointRequest != null && !string.IsNullOrEmpty(stsResponse))
                {
                    sharepointRequest.Method = "POST";
                    sharepointRequest.ContentType = "application/x-www-form-urlencoded";
                    sharepointRequest.CookieContainer = new CookieContainer();
                    sharepointRequest.AllowAutoRedirect = false;

                    // format the information to submit to the SharePoint STS
                    var loginInfo = String.Format("wa=wsignin1.0&wctx={0}&wresult={1}", HttpUtility.UrlEncode(sharePointInfo.Wctx), HttpUtility.UrlEncode(stsResponse));

                    // convert the login information to bytes for submitting on the request stream
                    var loginInfoBytes = Encoding.UTF8.GetBytes(loginInfo);

                    // write the bytes to the request stream
                    using (var requestStream = sharepointRequest.GetRequestStream())
                    {
                        requestStream.Write(loginInfoBytes, 0, loginInfoBytes.Length);
                        requestStream.Close();
                    }

                    // retrieve the response from the SharePoint STS
                    using (var webResponse = sharepointRequest.GetResponse() as HttpWebResponse)
                    {
                        if (webResponse != null)
                        {
                            if (webResponse.Cookies != null && webResponse.Cookies.Count > 0)
                            {
                                // find the FedAuth cook and return it
                                var cookie = webResponse.Cookies.Cast<Cookie>().FirstOrDefault(instance => instance.Name == "FedAuth");
                                return cookie != null ? cookie.Value : String.Empty;
                            }

                            webResponse.Close();
                        }
                    }
                }
                // unable to find the FedAuth cookie
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string STSResponse(string sharepointUrl, string realm)
        {
            try
            {
                var samlService = ServiceLocator.Get<ISAMLAuthentication>();
                if (samlService == null) return string.Empty;

                SecurityTokenService sts = new TelligentSTS(samlService.Configuration);

                var requestMessage = new SignInRequestMessage(new Uri(sharepointUrl), realm);
                var responseMessage = FederatedPassiveSecurityTokenServiceOperations.ProcessSignInRequest(requestMessage, CurrentPrincipal(), sts);

                return responseMessage.Result;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static IPrincipal CurrentPrincipal()
        {
            if (HttpContext.Current != null) return HttpContext.Current.User;

            var username = IdentityProviderPlugin.Plugin.Configuration.GetString(IdentityProviderPlugin.PropertyId.DefaultIdentity);

            if (string.IsNullOrEmpty(username)) throw new Exception("Default Identity not set in SharePoint SAML Authentication plugin.");

            var identity = new GenericIdentity(username);
            var currentPrincipal = new GenericPrincipal(identity, new string[0]);

            return currentPrincipal;
        }
    }
}
