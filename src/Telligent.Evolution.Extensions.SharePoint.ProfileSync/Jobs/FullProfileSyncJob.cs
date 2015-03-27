using System;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Data;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Managers;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs
{
    public class FullProfileSyncJob : IRecurringEvolutionJobPlugin
    {
        #region IRecurringEvolutionJobPlugin

        public string Name
        {
            get { return "SharePoint Full Profile Sync Job"; }
        }

        public string Description
        {
            get { return ""; }
        }

        public void Initialize() { }

        public JobSchedule DefaultSchedule
        {
            get { return JobSchedule.Daily(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 3, 0, 0)); }
        }

        public Guid JobTypeId
        {
            get { return new Guid("C7350CA5-B054-4692-904A-8A486B759A34"); }
        }

        public JobContext SupportedContext
        {
            get { return JobContext.Service; }
        }

        public void Execute(JobData jobData)
        {
            foreach (var spprofileSyncProvider in ProfileSyncHelper.ProviderList())
            {
                try
                {
                    using (var syncService = new SPProfileSyncService(spprofileSyncProvider))
                    {
                        if (!syncService.Enabled) continue;

                        var fullProfileSyncManager = new FullProfileSyncManager(syncService.Get<IFullProfileSyncService>(), spprofileSyncProvider.Id);
                        
                        if (!fullProfileSyncManager.IsSyncEnabled) continue;
                        
                        using (new PerformanceProfiler(String.Format("Full Profile Sync Job for {0}.", spprofileSyncProvider.SPSiteURL)))
                        {
                            fullProfileSyncManager.Sync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    SPLog.BackgroundJobError(ex, "Full Profile Sync error for url: {0}", spprofileSyncProvider.SPSiteURL);
                }
            }
        }

        #endregion
    }
}
