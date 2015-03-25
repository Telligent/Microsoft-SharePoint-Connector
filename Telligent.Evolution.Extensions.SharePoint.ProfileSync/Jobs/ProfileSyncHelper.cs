using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Plugins;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs
{
    public static class ProfileSyncHelper
    {
        public static IEnumerable<SPProfileSyncProvider> ProviderList()
        {
            try
            {
                var plugin = SPProfileSyncPlugin.Plugin;
                if (plugin != null)
                {
                    string spprofileSyncSettingsXml = plugin.Configuration.GetString(SPProfileSyncPlugin.PropertyId.SPProfileSyncSettings);
                    return new SPProfileSyncProviderList(spprofileSyncSettingsXml).All();
                }
            }
            catch (Exception ex)
            {
                SPLog.BackgroundJobError(ex, "Error loading Profile Sync settings");
            }
            return Enumerable.Empty<SPProfileSyncProvider>();
        }
    }
}
