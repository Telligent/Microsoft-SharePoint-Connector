using System;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi
{
    public class SPProfileSyncService : IDisposable
    {
        private const string SyncEnabledPropertyKey = "TEUserSyncEnable";

        private readonly SPProfileSyncProvider syncSettings;
        private readonly SPContext spcontext;
        private IProfileSyncService profileSyncService;

        private bool? isSyncEnabled = null;

        public SPProfileSyncService(SPProfileSyncProvider settings)
        {
            syncSettings = settings;
            spcontext = new SPContext(syncSettings.SPSiteURL, syncSettings.Authentication);
            InitService(settings);
        }

        public bool Enabled
        {
            get
            {
                if (isSyncEnabled == null)
                {
                    try
                    {
                        if (syncSettings.SyncConfig != null)
                        {
                            isSyncEnabled = syncSettings.SyncConfig.SyncEnabled;
                        }
                        else
                        {
                            SP.Web web = spcontext.Site.RootWeb;
                            spcontext.Load(web.AllProperties);

                            spcontext.ExecuteQuery();

                            isSyncEnabled = profileSyncService != null && web.AllProperties.FieldValues.ContainsKey(SyncEnabledPropertyKey) && Convert.ToBoolean(web.AllProperties.FieldValues[SyncEnabledPropertyKey]);
                        }
                    }
                    catch (Exception ex)
                    {
                        SPLog.RoleOperationUnavailable(ex, ex.Message);
                        return false;
                    }
                }
                return isSyncEnabled ?? false;
            }
        }

        public T Get<T>() where T : IProfileSyncService
        {
            return (T)profileSyncService;
        }

        #region IDisposable Members

        public void Dispose()
        {
            spcontext.Dispose();
            if (profileSyncService != null)
            {
                profileSyncService.Dispose();
            }
        }

        #endregion

        private void InitService(SPProfileSyncProvider settings)
        {
            if (profileSyncService == null)
            {
                var siteUserProfile = new SiteUserProfileService(settings);
                if (siteUserProfile.Enabled)
                {
                    profileSyncService = siteUserProfile;
                    return;
                }
                siteUserProfile.Dispose();
            }

            if (profileSyncService == null)
            {
                var farmUserProfile = new FarmUserProfileService(settings);
                if (farmUserProfile.Enabled)
                {
                    profileSyncService = farmUserProfile;
                    return;
                }
                farmUserProfile.Dispose();
            }
        }
    }
}