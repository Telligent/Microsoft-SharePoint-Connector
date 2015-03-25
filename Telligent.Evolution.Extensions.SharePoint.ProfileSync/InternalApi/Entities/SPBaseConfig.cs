using System.Collections.Generic;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities
{
    public class SPBaseConfig
    {
        internal SPBaseConfig() { }

        public bool SyncEnabled { get; set; }
        public bool FarmSyncEnabled { get; set; }
        public List<UserFieldMapping> SiteProfileMappedFields { get; set; }
        public List<UserFieldMapping> FarmProfileMappedFields { get; set; }
    }
}
