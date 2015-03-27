using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Data;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Managers
{
    public class IncrementalProfileSyncManager : ProfileSyncManager
    {
        private const int DefaultDaysRangeForIncrementalSyncing = 6;

        private readonly IIncrementalProfileSyncService incrementalProfileSyncService;

        public IncrementalProfileSyncManager(IIncrementalProfileSyncService incrementalProfileSyncService, int internalProviderId)
            : base(incrementalProfileSyncService, internalProviderId)
        {
            this.incrementalProfileSyncService = incrementalProfileSyncService;
        }

        public override void Sync()
        {
            var mappedFields = incrementalProfileSyncService.Fields.ToList();

            if (mappedFields.Count <= 0) { return; }

            var externalFields = new List<string>();
            var internalFields = new List<string>();
            var lastRunTimeUtcDate = GetAndResetLastRunTime();

            SplitMappedFields(mappedFields, internalFields, externalFields);

            try
            {
                // Get LastUpdated external Users
                var externalUsersLastUpdated = incrementalProfileSyncService.List(lastRunTimeUtcDate);

                // Get LastUpdated internal Users
                var internalApiUsers = PublicApi.Users.List(new UsersListOptions { LastUpdatedUtcDate = lastRunTimeUtcDate });
                var internalUserLastUpdated = InitInternalUserList(internalApiUsers);

                // Sync last updated profiles
                SyncLastUpdatedUsers(internalUserLastUpdated, internalFields, externalUsersLastUpdated, externalFields, mappedFields);

                UpdateLastRunStatus(Status.Succeeded);
            }
            catch
            {
                UpdateLastRunStatus(Status.Failed);
            }
        }

        private void SyncLastUpdatedUsers(List<InternalApi.Entities.TEApiUser> internalUserLastUpdated, List<string> internalFields, List<InternalApi.Entities.User> externalUsersLastUpdated, List<string> externalFields, List<InternalApi.Entities.UserFieldMapping> mappedFields)
        {
            // Sync Last Updated External Users
            foreach (var externalUser in externalUsersLastUpdated)
            {
                var internalUser = internalUserLastUpdated.FirstOrDefault(u => u.Email.Equals(externalUser.Email));
                if (internalUser == null)
                {
                    var internalApiUser = PublicApi.Users.Get(new UsersGetOptions { Email = externalUser.Email });
                    if (internalApiUser == null) continue;

                    internalUser = InitInternalUser(internalApiUser);
                }
                
                MergeAndUpdate(internalUser, internalFields, externalUser, externalFields, mappedFields);
                internalUserLastUpdated.RemoveAll(p => p.Email.Equals(externalUser.Email));
            }

            // Sync Last Updated Internal Users except Users, which were updated
            var externalUserEmailList = internalUserLastUpdated.Select(u => u.Email).Except(externalUsersLastUpdated.Select(u => u.Email)).ToList();
            var externalUsersUpdatedInTE = incrementalProfileSyncService.List(externalUserEmailList);

            foreach (var externalUser in externalUsersUpdatedInTE)
            {
                var internalUser = internalUserLastUpdated.FirstOrDefault(u => u.Email.Equals(externalUser.Email));
                if (internalUser == null) continue;

                MergeAndUpdate(internalUser, internalFields, externalUser, externalFields, mappedFields);
            }
        }

        private DateTime GetAndResetLastRunTime()
        {
            DateTime? lastUpdatedUtcDate = null;

            try
            {
                Status syncStatus;

                if (ProfileSyncController.GetLastRunTime(InternalProviderId, out lastUpdatedUtcDate, out syncStatus) && syncStatus == Status.Succeeded)
                {
                    ProfileSyncController.ResetLastRunTime(InternalProviderId, Status.InProgress);
                }
                else
                {
                    ProfileSyncController.SetLastRunStatus(InternalProviderId, Status.InProgress);
                }
            }
            catch (Exception)
            {
                SPLog.Event("Cound not get last Profile Incremental Sync time, using default.");
            }

            return lastUpdatedUtcDate ?? DefaultLastRunTime();
        }

        private void UpdateLastRunStatus(Status syncStatus)
        {
            ProfileSyncController.SetLastRunStatus(InternalProviderId, syncStatus);
        }

        private DateTime DefaultLastRunTime()
        {
            return DateTime.UtcNow.AddDays(-DefaultDaysRangeForIncrementalSyncing);
        }
    }
}
