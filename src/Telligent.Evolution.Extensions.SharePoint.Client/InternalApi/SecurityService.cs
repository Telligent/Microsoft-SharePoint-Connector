using System;
using System.Data;
using System.Data.SqlClient;
using Telligent.Evolution.Components;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal static class SecurityService
    {
        public class SecureItem : ISecuredItem
        {
            public Guid NodeId { get; set; }

            public Guid ApplicationId
            {
                get { return Guid.Parse("85A4713C-8CE0-4CD7-BD9E-C1F663810FDB"); }
            }

            public Guid ApplicationTypeId
            {
                get { return Guid.Parse("89CD418E-3EE3-47C9-A7AE-DCC27D4E0CA6"); }
            }
        }

        private static readonly ISecurityService securityService = Telligent.Common.Services.Get<ISecurityService>();

        public static void RecalculatePermissions(Guid applicationId, Guid applicationTypeId, Guid groupApplicationId)
        {
            // TODO: Replace with PublicApi when it will be available
            using (var connection = GetSqlConnection())
            {
                using (var command = CreateSprocCommand("[te_SharePoint_Node_AddUpdate]", connection))
                {
                    command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = applicationId;
                    command.Parameters.Add("@ApplicationTypeId", SqlDbType.UniqueIdentifier).Value = applicationTypeId;
                    command.Parameters.Add("@GroupApplicationId", SqlDbType.UniqueIdentifier).Value = groupApplicationId;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            securityService.RecalculatePermissions(new SecureItem { NodeId = applicationId });
        }

        #region Helpers
        private static SqlConnection GetSqlConnection()
        {
            return new SqlConnection(SharePointDataService.ConnectionString);
        }

        private static SqlCommand CreateSprocCommand(string sprocName, SqlConnection connection)
        {
            return new SqlCommand("dbo." + sprocName, connection) { CommandType = CommandType.StoredProcedure };
        }
        #endregion
    }
}
