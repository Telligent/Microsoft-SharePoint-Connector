using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using System;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model
{
    public class SPWeb
    {
        public Guid WebId { get; private set; }
        public Guid SiteId { get; private set; }
        public string Title { get; private set; }
        public string Url { get; private set; }
        public bool IsMapped { get; set; }
        public bool IsSite { get; set; }

        public SPWeb() { }

        public SPWeb(Guid siteId, Guid webId)
        {
            WebId = webId;
            SiteId = siteId;
        }

        public SPWeb(string url, Guid siteId, Guid webId, string title)
            : this(siteId, webId)
        {
            WebId = webId;
            SiteId = siteId;
            Title = title;
            Url = url;
        }

        #region Overriden

        public override bool Equals(object obj)
        {
            var web = obj as SPWeb;

            if (web != null &&
                web.SiteId == SiteId &&
                web.WebId == WebId)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return WebId.GetHashCode() ^ SiteId.GetHashCode();
        }

        #endregion
    }
}
