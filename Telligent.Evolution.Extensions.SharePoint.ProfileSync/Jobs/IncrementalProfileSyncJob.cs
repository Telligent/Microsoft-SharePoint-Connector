using System;
using System.Globalization;
using Telligent.Evolution.Extensibility.Jobs.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Data;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Managers;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs
{
    public class IncrementalProfileSyncJob : IRecurringEvolutionJobPlugin
    {
        #region IRecurringEvolutionJobPlugin

        public string Name
        {
            get { return "SharePoint Incremental Profile Sync Job"; }
        }

        public string Description
        {
            get { return ""; }
        }

        public void Initialize() { }

        public JobSchedule DefaultSchedule
        {
            get { return JobSchedule.EveryMinutes(5); }
        }

        public Guid JobTypeId
        {
            get { return new Guid("F6F322CD-6D97-44A6-AC30-79F173C9AD45"); }
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

                        var incrementalProfileSyncManager = new IncrementalProfileSyncManager(syncService.Get<IIncrementalProfileSyncService>(), spprofileSyncProvider.Id);

                        if (!incrementalProfileSyncManager.IsSyncEnabled) continue;

                        using (new PerformanceProfiler(String.Format("Incremental Profile Sync Job for {0}.", spprofileSyncProvider.SPSiteURL)))
                        {
                            incrementalProfileSyncManager.Sync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    SPLog.BackgroundJobError(ex, "Incremental Profile Sync Error. SPSite url: {0}", spprofileSyncProvider.SPSiteURL);
                }
            }
        }

        #endregion
    }
}
