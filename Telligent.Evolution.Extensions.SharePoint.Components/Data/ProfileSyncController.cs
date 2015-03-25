using System;
using System.Data.SqlClient;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Data
{
    public class ProfileSyncController : BaseDataProvider
    {
        public static bool GetLastRunTime(int providerId, out DateTime? lastRunTime, out Status syncStatus)
        {
            lastRunTime = null;
            syncStatus = Status.Failed;

            const string spGetLastRunTime = "te_SharePoint_ProfileSync_GetLastRunTime";
            try
            {
                var providerIdParam = new SqlParameter("@ProviderId", providerId);
                var lastRunTimeParam = new SqlParameter("@LastRunTime", System.Data.SqlDbType.DateTime)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                var syncStatusParam = new SqlParameter("@SyncStatus", System.Data.SqlDbType.Int)
                {
                    Direction = System.Data.ParameterDirection.Output
                };

                ExecuteScalar(spGetLastRunTime, GetConnection(), providerIdParam, lastRunTimeParam, syncStatusParam);

                lastRunTime = lastRunTimeParam.Value as DateTime?;
                syncStatus = (Status)syncStatusParam.Value;

                return true;
            }
            catch (Exception ex)
            {
                SPLog.UserProfileUpdated(ex, string.Format("Profile Sync Error. {0}: {1}", spGetLastRunTime, providerId));
            }
            return false;
        }

        public static void ResetLastRunTime(int providerId, Status syncStatus)
        {
            const string spResetLastRunTime = "te_SharePoint_ProfileSync_ResetLastRunTime";
            try
            {
                var providerIdParam = new SqlParameter("@ProviderId", providerId);
                var syncStatusParam = new SqlParameter("@SyncStatus", (int)syncStatus);
                var lastRunTimeParam = new SqlParameter("@LastRunTime", System.Data.SqlDbType.DateTime)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                ExecuteNonQuery(spResetLastRunTime, GetConnection(), providerIdParam, lastRunTimeParam, syncStatusParam);
            }
            catch (Exception ex)
            {
                SPLog.UserProfileUpdated(ex, string.Format("Profile Sync Error. {0}: {1}", spResetLastRunTime, providerId));
            }
        }

        public static void SetLastRunStatus(int providerId, Status syncStatus)
        {
            const string spResetLastRunStatus = "te_SharePoint_ProfileSync_SetLastRunStatus";
            try
            {
                var providerIdParam = new SqlParameter("@ProviderId", providerId);
                var syncStatusParam = new SqlParameter("@SyncStatus", (int)syncStatus);
                ExecuteNonQuery(spResetLastRunStatus, GetConnection(), providerIdParam, syncStatusParam);
            }
            catch (Exception ex)
            {
                SPLog.UserProfileUpdated(ex, string.Format("Profile Sync Error. {0}: {1}", Enum.GetName(typeof(Status), syncStatus), providerId));
            }
        }
    }
}
