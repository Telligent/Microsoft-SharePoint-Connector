using System.Collections.Generic;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities
{
    internal class SPConfiguration : SPBaseConfig
    {
        internal SPConfiguration() { }

        public SPConfiguration(string url, Authentication auth)
        {
            Url = url;
            Auth = auth;
        }

        public string Url { get; private set; }
        public Authentication Auth { get; private set; }
        public List<ProfileField> SiteProfileFields { get; set; }
        public List<ProfileField> FarmProfileFields { get; set; }
    }
}
