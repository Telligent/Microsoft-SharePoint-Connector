using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Managers
{
    public abstract class ProfileSyncManager
    {
        protected readonly IProfileSyncService ProfileSyncService;
        protected readonly int InternalProviderId;

        protected ProfileSyncManager(IProfileSyncService profileSyncService, int internalProviderId)
        {
            ProfileSyncService = profileSyncService;
            InternalProviderId = internalProviderId;
        }

        public bool IsSyncEnabled
        {
            get
            {
                return ProfileSyncService != null && ProfileSyncService.Enabled;
            }
        }

        public abstract void Sync();

        protected void SplitMappedFields(IEnumerable<InternalApi.Entities.UserFieldMapping> mappedFields, ICollection<string> internalFields, ICollection<string> externalFields)
        {
            foreach (InternalApi.Entities.UserFieldMapping map in mappedFields)
            {
                if (map.SyncDirection == InternalApi.Entities.SyncDirection.Export)
                {
                    internalFields.Add(map.InternalUserFieldId);
                }
                else if (map.SyncDirection == InternalApi.Entities.SyncDirection.Import)
                {
                    externalFields.Add(map.ExternalUserFieldId);
                }
            }
        }

        protected InternalApi.Entities.TEApiUser InitInternalUser(User internalUser)
        {
            var teApiUser = new InternalApi.Entities.TEApiUser(ExecuteProfileSyncHelper.TelligentId, ExecuteProfileSyncHelper.TelligentEmail, internalUser)
            {
                Fields = ExecuteProfileSyncHelper.GetInternalUserFields(internalUser)
            };

            if (internalUser.ExtendedAttributes == null) return teApiUser;

            foreach (var extendedAttr in internalUser.ExtendedAttributes)
            {
                teApiUser.ExtendedAttributes.Add(extendedAttr.Key, extendedAttr.Value);
            }

            return teApiUser;
        }

        protected List<InternalApi.Entities.TEApiUser> InitInternalUserList(IEnumerable<User> internalUserList)
        {
            return internalUserList.Select(InitInternalUser).ToList();
        }

        protected void MergeAndUpdate(InternalApi.Entities.TEApiUser internalUser, List<string> internalFields, InternalApi.Entities.User externalUser, List<string> externalFields, List<InternalApi.Entities.UserFieldMapping> mappedFields)
        {
            var mergeResult = ExecuteProfileSyncHelper.MergeUsers(externalUser, internalUser, mappedFields);

            try
            {
                if ((mergeResult & ExecuteProfileSyncHelper.MergeResult.InternalUpdated) == ExecuteProfileSyncHelper.MergeResult.InternalUpdated)
                {
                    ExecuteProfileSyncHelper.UpdateInternalUser(internalUser, internalFields);
                }
            }
            catch (Exception ex)
            {
                var msg = string.Format("Error updating internal user. User id = {0}/{1}", internalUser.Id, internalUser.Email ?? string.Empty);
                SPLog.UserProfileUpdated(ex, msg);
            }

            try
            {
                if ((mergeResult & ExecuteProfileSyncHelper.MergeResult.ExternalUpdated) == ExecuteProfileSyncHelper.MergeResult.ExternalUpdated)
                {
                    ProfileSyncService.Update(externalUser, externalFields);
                }
            }
            catch (Exception ex)
            {
                var msg = string.Format("Error updating external user. User id = {0}/{1}", externalUser.Id, externalUser.Email ?? string.Empty);
                SPLog.UserProfileUpdated(ex, msg);
            }
        }
    }
}
