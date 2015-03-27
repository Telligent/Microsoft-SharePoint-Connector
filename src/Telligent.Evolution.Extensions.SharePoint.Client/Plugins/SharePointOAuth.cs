using Microsoft.IdentityModel.S2S.Protocols.OAuth2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls.PropertyRules;
using Telligent.Evolution.Extensibility.Authentication.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins
{
    public class SharePointOAuth : IOAuthClient, IRequiredConfigurationPlugin, ICategorizedPlugin, ITranslatablePlugin, IApplicationNavigable, Extensibility.Urls.Version1.INavigableApplicationType
    {
        private static readonly Guid Id = new Guid("281C240A-9DAA-4147-BF17-5819E137F79F");

        private const string TokenKey = "FedAuth";
        private const string TargetPrincipalName = "00000003-0000-0ff1-ce00-000000000000";
        private ITranslatablePluginController translationController;
        private string callbackUrl;

        public virtual string Office365Url { get { return Configuration == null ? string.Empty : Configuration.GetString("Office365Url"); } }

        #region IOAuthClient

        public string ClientType { get { return "sharepoint"; } }

        public virtual string ConsumerKey { get { return Configuration == null ? string.Empty : Configuration.GetString("ConsumerKey"); } }

        public virtual string ConsumerSecret { get { return Configuration == null ? string.Empty : Configuration.GetString("ConsumerSecret"); } }

        public virtual string CallbackUrl
        {
            get
            {
                return callbackUrl;
            }
            set
            {
                if (!string.IsNullOrEmpty(value) && value.StartsWith("http:"))
                    callbackUrl = "https" + value.Substring(4);
                else
                    callbackUrl = value;
            }
        }

        public string ThemeColor { get { return "3B5998"; } }

        public string ClientName { get { return translationController.GetLanguageResourceValue("OAuth_SharePoint_Name"); } }

        public string Privacy { get { return translationController.GetLanguageResourceValue("OAuth_SharePoint_Privacy"); } }

        public string ClientLogoutScript
        {
            get { return null; }
        }

        public string IconUrl
        {
            get { return "s/icons/office365.png"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string GetAuthorizationLink()
        {
            var redirect = RemoveParameter(CallbackUrl, "ReturnUrl");
            return string.Format("{0}/_layouts/15/OAuthAuthorize.aspx?client_id={1}&scope=Site.Manage%20Web.Manage%20List.Manage&response_type=code&redirect_uri={2}", Office365Url, ConsumerKey, Globals.UrlEncode(redirect));
        }

        public OAuthData ProcessLogin(HttpContextBase context)
        {
            var accessToken = GetAccessToken(context);
            if (string.IsNullOrEmpty(accessToken))
            {
                // Get Access Token
                var authCode = context.Request.QueryString["code"];
                var errorMsg = context.Request.QueryString["error"];

                if (!Enabled || !string.IsNullOrEmpty(errorMsg) || string.IsNullOrEmpty(authCode))
                {
                    FailedLogin();
                }

                // Remove code from Uri
                if (context.Request.Url != null)
                {
                    CallbackUrl = RemoveParameter(context.Request.Url.AbsoluteUri, "code");
                }

                //Get the access token
                var tokens = GetAccessToken(authCode);
                if (tokens == null)
                {
                    FailedLogin();
                }

                StoreAccessToken(tokens.AccessToken, tokens.ExpiresOn, context);

                if (context.Request.Url != null)
                {
                    CallbackUrl = string.Format("/login?ReturnUrl={0}&oauth_data_token_key=TOKEN", Globals.UrlEncode(context.Request.Url.GetLeftPart(UriPartial.Authority)));
                }
            }
            return GetUserData();
        }

        public OAuth2AccessTokenResponse GetAccessToken(string authCode)
        {
            var realm = GetRealmFromTargetUrl(Office365Url);
            var targetHost = new Uri(Office365Url).Authority;
            var resource = GetFormattedPrincipal(TargetPrincipalName, targetHost, realm);
            var clientId = GetFormattedPrincipal(ConsumerKey, null, realm);

            var oauth2Request = OAuth2MessageFactory.CreateAccessTokenRequestWithAuthorizationCode(
                clientId,
                ConsumerSecret,
                Globals.UrlEncode(authCode),
                resource);

            oauth2Request.RedirectUri = CallbackUrl;

            try
            {
                var client = new OAuth2S2SClient();
                var oauth2Response = client.Issue(GetStsUrl(realm), oauth2Request) as OAuth2AccessTokenResponse;
                if (oauth2Response != null) return oauth2Response;
            }
            catch (WebException wex)
            {
                using (var sr = new StreamReader(wex.Response.GetResponseStream()))
                {
                    var responseText = sr.ReadToEnd();
                    throw new WebException(wex.Message + " - " + responseText, wex);
                }
            }

            return null;
        }

        private void FailedLogin()
        {
            throw new CSException(CSExceptionType.OAuthLoginFailed);
        }

        private OAuthData GetUserData()
        {
            //We now have the credentials, so we can start making API calls
            using (var context = new SPContext(Office365Url, new Components.AuthenticationUtil.Methods.OAuth()))
            {
                var userDetails = context.Web.CurrentUser;

                context.Load(userDetails,
                    usr => usr.Email,
                    usr => usr.Id,
                    usr => usr.LoginName,
                    usr => usr.Title,
                    usr => usr.UserId);

                context.ExecuteQuery();

                var uid = userDetails.Id.ToString(CultureInfo.InvariantCulture);
                var data = new OAuthData
                {
                    ClientId = uid,
                    ClientType = ClientType,
                    Email = userDetails.Email,
                    UserName = SanitizeUserName(userDetails.LoginName),
                    AvatarUrl = string.Empty,
                    CommonName = userDetails.Title
                };

                return data;
            }
        }

        private static string SanitizeUserName(string userName)
        {
            return userName.Substring(userName.LastIndexOf('|') + 1);
        }

        private static string GetAccessToken(HttpContextBase context)
        {
            var token = context.Request.Cookies.Get(TokenKey);
            return token != null && !string.IsNullOrEmpty(token.Value) ? token.Value : null;
        }

        private static void StoreAccessToken(string accessToken, DateTime expiresOn, HttpContextBase context)
        {
            context.Response.Cookies.Add(new HttpCookie(TokenKey, accessToken) { Expires = expiresOn });
        }

        #endregion

        #region Plugin

        public string Name { get { return "SharePoint OAuth Client"; } }

        public string Description { get { return "Provides user OAuth for SharePoint 2013 or Office 365"; } }

        public void Initialize() { }

        #endregion

        #region IConfigurablePlugin

        protected IPluginConfiguration Configuration { get; private set; }

        public void Update(IPluginConfiguration configuration)
        {
            Configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                PropertyGroup[] groups = { new PropertyGroup("options", "Options", 0) };

                var consumerKey = new Property("ConsumerKey", "Client Id", PropertyType.String, 0, "");
                consumerKey.Rules.Add(new PropertyRule(typeof(TrimStringRule), false));
                groups[0].Properties.Add(consumerKey);

                var consumerSecret = new Property("ConsumerSecret", "Client Secret", PropertyType.String, 0, "");
                consumerSecret.Rules.Add(new PropertyRule(typeof(TrimStringRule), false));
                groups[0].Properties.Add(consumerSecret);

                groups[0].Properties.Add(new Property("Office365Url", "Office365 URL", PropertyType.Url, 0, ""));

                return groups;
            }
        }

        #endregion

        #region IRequiredConfigurationPlugin

        public bool IsConfigured
        {
            get
            {
                return !string.IsNullOrEmpty(ConsumerKey) && !string.IsNullOrEmpty(ConsumerSecret) && !string.IsNullOrEmpty(Office365Url);
            }
        }

        #endregion

        #region ICategorizedPlugin

        public string[] Categories { get { return new[] { "OAuth", "SharePoint" }; } }

        #endregion

        #region ITranslatablePlugin

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set("OAuth_SharePoint_Name", "SharePoint");
                t.Set("OAuth_SharePoint_Privacy", "By signing in with SharePoint, data from your profile, such as your name, userID, and email address, will be collected so that an account can be created for you.  Your SharePoint password will not be collected.  Please click on the link at the bottom of the page to be directed to our privacy policy for information on how the collected data will be protected.");

                return new[] { t };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        #region Helper Methods

        private static string RemoveParameter(string url, string param)
        {
            var cleanUrl = new Uri(url);
            var query = HttpUtility.ParseQueryString(cleanUrl.Query);

            query.Remove(param);

            return string.Concat(cleanUrl.GetLeftPart(UriPartial.Path), query.HasKeys() ? string.Concat("?", query) : string.Empty);
        }

        private static string GetFormattedPrincipal(string principalName, string hostName, string realm)
        {
            return !String.IsNullOrEmpty(hostName) ?
                String.Format(CultureInfo.InvariantCulture, "{0}/{1}@{2}", principalName, hostName, realm) :
                String.Format(CultureInfo.InvariantCulture, "{0}@{1}", principalName, realm);
        }

        private static string GetStsUrl(string realm)
        {
            var document = GetMetadataDocument(realm);
            var endpoint = document.Endpoints.SingleOrDefault(e => e.Protocol == "OAuth2");

            if (null != endpoint)
            {
                return endpoint.Location;
            }

            throw new Exception("Metadata document does not contain STS endpoint url");
        }

        private static string GetRealmFromTargetUrl(string targetApplication)
        {
            var targetApplicationUri = new Uri(targetApplication);
            var request = WebRequest.Create(targetApplicationUri + "/_vti_bin/client.svc");
            request.Headers.Add("Authorization: Bearer ");

            try
            {
                using (WebResponse response = request.GetResponse()) { }
            }
            catch (WebException e)
            {
                string bearerResponseHeader = e.Response.Headers["WWW-Authenticate"];
                string realm = bearerResponseHeader.Substring(bearerResponseHeader.IndexOf("Bearer realm=\"") + 14, 36);

                Guid realmGuid;

                if (Guid.TryParse(realm, out realmGuid))
                {
                    return realm;
                }

            }
            return null;
        }

        private static JsonMetadataDocument GetMetadataDocument(string realm)
        {
            string acsMetadataEndpointUrlWithRealm = String.Format(CultureInfo.InvariantCulture, "{0}?realm={1}", "https://accounts.accesscontrol.windows.net/metadata/json/1", realm);
            byte[] acsMetadata;
            using (var webClient = new WebClient())
            {
                acsMetadata = webClient.DownloadData(acsMetadataEndpointUrlWithRealm);
            }

            string jsonResponseString = Encoding.UTF8.GetString(acsMetadata);

            var serializer = new JavaScriptSerializer();
            var document = serializer.Deserialize<JsonMetadataDocument>(jsonResponseString);

            if (null == document)
            {
                throw new Exception("No metadata document found at the global endpoint " + acsMetadataEndpointUrlWithRealm);
            }

            return document;
        }

        private class JsonMetadataDocument
        {
            public string ServiceName { get; set; }
            public List<JsonEndpoint> Endpoints { get; set; }
        }

        private class JsonEndpoint
        {
            public string Location { get; set; }
            public string Protocol { get; set; }
            public string Usage { get; set; }
        }

        #endregion

        public static SharePointOAuth Plugin
        {
            get
            {
                return PluginManager.Get<SharePointOAuth>().FirstOrDefault();
            }
        }

        #region INavigableApplicationType

        public Guid ApplicationTypeId
        {
            get { return Id; }
        }

        public string ApplicationTypeName
        {
            get { return "SharePoint OAuth"; }
        }

        public string PathDelimiter
        {
            get { return "s"; }
        }

        public void AttachChangeEvents(IApplicationStateChanges stateChanges) { }

        public Guid[] ContainerTypes
        {
            get { return new[] { ContentTypes.Group }; }
        }

        public IApplication Get(Guid applicationId)
        {
            return null;
        }

        #endregion

        #region IApplicationNavigable

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddRaw("office365_icon_handler", "icons/office365.png", null, null,
               (a, p) =>
               {
                   var handler = new OAuthIconHandler
                   {
                       CacheTimeOut = TimeSpan.FromMinutes(5)
                   };
                   handler.ProcessRequest(a.ApplicationInstance.Context);
               }, new RawDefinitionOptions() { ParseContext = null });
        }
        #endregion

        internal class OAuthIconHandler : IHttpHandler
        {
            private readonly InternalApi.ICacheService cacheService;

            private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
            public TimeSpan CacheTimeOut
            {
                get { return cacheTimeOut; }
                set { cacheTimeOut = value; }
            }

            public OAuthIconHandler() : this(ServiceLocator.Get<InternalApi.ICacheService>()) { }
            public OAuthIconHandler(InternalApi.ICacheService cacheService)
            {
                this.cacheService = cacheService;
            }

            public bool IsReusable
            {
                get { return true; }
            }

            public void ProcessRequest(HttpContext context)
            {
                const string cacheId = "Office365Icon";
                var iconContent = (byte[])cacheService.Get(cacheId, Extensibility.Caching.Version1.CacheScope.Context | Extensibility.Caching.Version1.CacheScope.Process);
                if (iconContent == null)
                {
                    var assemblyName = GetType().Assembly.GetName().Name;
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var stream = EmbeddedResources.GetStream(assemblyName + ".Resources.OAuth.office365.png"))
                        {
                            stream.CopyTo(memoryStream);
                        }
                        iconContent = memoryStream.ToArray();
                    }
                    cacheService.Put(cacheId, iconContent, Extensibility.Caching.Version1.CacheScope.Context | Extensibility.Caching.Version1.CacheScope.Process, null, CacheTimeOut);
                }
                context.Response.ContentType = "image/png";
                context.Response.BinaryWrite(iconContent);
            }
        }
    }
}
