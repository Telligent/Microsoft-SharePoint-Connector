using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Data;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Managers
{
    public class FullProfileSyncManager : ProfileSyncManager
    {
        private readonly IFullProfileSyncService fullProfileSyncService;

        public FullProfileSyncManager(IFullProfileSyncService fullProfileSyncService, int internalProviderId)
            : base(fullProfileSyncService, internalProviderId)
        {
            this.fullProfileSyncService = fullProfileSyncService;
        }

        public override void Sync()
        {
            var mappedFields = fullProfileSyncService.Fields.ToList();
            
            if (mappedFields.Count <= 0) { return; }

            var externalFields = new List<string>();
            var externalUsersCount = 0;
            var internalFields = new List<string>();
            var missingUserCount = 0;
            var missingUsers = new List<string>();
            var nextIndex = 0;

            SplitMappedFields(mappedFields, internalFields, externalFields);

            do
            {
                var externalUsers = fullProfileSyncService.List(ref nextIndex);
                externalUsersCount += externalUsers.Count;

                if (externalUsers.Count <= 0) break;

                // sync every external user
                foreach(var externalUser in externalUsers)
                {
                    var internalApiUser = PublicApi.Users.Get(new UsersGetOptions { Email = externalUser.Email });
                    if (internalApiUser != null)
                    {
                        var internalUser = InitInternalUser(internalApiUser);
                        MergeAndUpdate(internalUser, internalFields, externalUser, externalFields, mappedFields);
                    }
                    else 
                    {
                        if (missingUsers.Count < 100) { missingUsers.Add(externalUser.Email); }
                        missingUserCount++;
                    }
                }
            } while (true);

            SPLog.Info(string.Format("Profile Sync found {0} SharePoint user(s)", externalUsersCount));

            if (missingUsers.Count > 0)
            {
                SPLog.Event( string.Format("Profile Sync could not find following Evolution user(s) {0}. Total not found: {1}", string.Join(", ", missingUsers.ToArray()), missingUserCount));
                UpdateLastRunStatus(Status.Failed);
            }
            else
            {
                UpdateLastRunStatus(Status.Succeeded);
            }
        }

        private void UpdateLastRunStatus(Status syncStatus)
        {
            ProfileSyncController.SetLastRunStatus(InternalProviderId, syncStatus);
        }
    }
}
