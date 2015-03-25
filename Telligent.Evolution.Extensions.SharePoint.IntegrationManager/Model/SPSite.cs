using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager
{
    public class SPSite
    {
        private readonly string siteUrl;
        private readonly Authentication auth;

        public SPSite(string url, Authentication auth)
        {
            siteUrl = url;
            this.auth = auth;
        }

        public List<string> LoadSubSiteUrls()
        {
            var webCollection = new List<string>();

            using (var clientContext = new SPContext(siteUrl, auth, runAsServiceAccount: true))
            {
                var webs = clientContext.Web.Webs;
                clientContext.Load(webs);

                try
                {
                    clientContext.ExecuteQuery();
                }
                catch (Exception ex)
                {
                    SPLog.UnKnownError(ex, "An exception in the process of SiteCollection loading of type {0} has been occurred. The exception message is: {1}", ex.GetType().Name, ex.Message);
                    return webCollection;
                }

                var baseUrl = clientContext.Url.Trim('/');

                foreach (var web in webs)
                {
                    webCollection.Add(MergeUrl(baseUrl, web.ServerRelativeUrl));
                }
            }

            return webCollection;
        }

        public static SPWeb OpenWeb(string url, Authentication auth)
        {
            var site = new SPSite(url, auth);
            return site.OpenWeb();
        }

        public SPWeb OpenWeb()
        {
            try
            {
                using (var clientContext = new SPContext(siteUrl, auth, runAsServiceAccount: true))
                {
                    var web = clientContext.Web;
                    var site = clientContext.Site;

                    clientContext.Load(web, w => w.Title, w => w.Id);
                    clientContext.Load(site, s => s.Id);

                    clientContext.ExecuteQuery();

                    return new SPWeb(clientContext.Url, site.Id, web.Id, web.Title);
                }
            }
            catch
            {
                return null;
            }
        }

        public bool IsSite()
        {
            try
            {
                using (var clientContext = new SPContext(siteUrl, auth, runAsServiceAccount: true))
                {
                    var web = clientContext.Web;
                    clientContext.Load(web);
                    clientContext.ExecuteQuery();
                }
            }
            catch (ClientRequestException)
            {
                // site does not existed
                return false;
            }
            catch (WebException)
            {
                // credentials are invalid
                return false;
            }
            catch (Exception)
            {
                // unhandled exception
                return false;
            }

            return true;
        }

        public static string MergeUrl(string baseUrl, string webUrl)
        {
            try
            {
                return string.Format("{0}/{1}", baseUrl, webUrl.Split('/').Last());
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
