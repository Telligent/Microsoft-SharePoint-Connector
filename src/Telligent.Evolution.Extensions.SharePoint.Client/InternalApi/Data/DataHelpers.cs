using System;
using System.Data;
using System.Data.SqlClient;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal static class DataHelpers
    {
        public static SqlConnection GetSqlConnection()
        {
            return new SqlConnection(SharePointDataService.ConnectionString);
        }

        public static SqlCommand CreateSprocCommand(string sprocName, SqlConnection connection)
        {
            return new SqlCommand("dbo." + sprocName, connection) { CommandType = CommandType.StoredProcedure };
        }

        public static Guid GetGuid(this SqlDataReader reader, string name)
        {
            var columnValue = reader[name];
            if (columnValue is Guid)
            {
                return (Guid)columnValue;
            }
            return Guid.Empty;
        }

        public static int GetInt(this SqlDataReader reader, string name)
        {
            var columnValue = reader[name];
            if (columnValue is int)
            {
                return (int)columnValue;
            }
            return default(int);
        }

        public static string GetStringOrEmpty(this SqlDataReader reader, string name)
        {
            var columnValue = reader[name];
            if (columnValue != null)
            {
                return columnValue.ToString();
            }
            return string.Empty;
        }

        public static DateTime GetDate(this SqlDataReader reader, string name)
        {
            var columnValue = reader[name];
            if (columnValue is DateTime)
            {
                return (DateTime)columnValue;
            }
            return default(DateTime);
        }
    }

    internal static class Validators
    {
        public static void Validate(this ListBase list)
        {
            if (list.GroupId < 0)
                throw new InvalidOperationException("The group identified is invalid.");

            if (list.Id == Guid.Empty)
                throw new InvalidOperationException("Id cannot be empty.");

            if (string.IsNullOrEmpty(list.SPWebUrl))
                throw new InvalidOperationException("Url cannot be empty.");
        }

        public static void Validate(this ItemBase item)
        {
            if (item.Id < 0)
                throw new InvalidOperationException("Id is invalid.");

            if (item.UniqueId == Guid.Empty)
                throw new InvalidOperationException("Item unique Id cannot be empty.");

            if (item.ApplicationId == Guid.Empty)
                throw new InvalidOperationException("Application Id cannot be empty.");
        }

        public static void ValidateGroupId(int groupId)
        {
            if (groupId < 0)
                throw new InvalidOperationException("The group identified is invalid.");
        }

        public static void ValidateApplicationId(Guid applicationId)
        {
            if (applicationId == Guid.Empty)
                throw new InvalidOperationException("Application Id cannot be empty.");
        }

        public static void ValidateApplicationKey(string applicationKey)
        {
            if (string.IsNullOrWhiteSpace(applicationKey))
                throw new InvalidOperationException("Application Key cannot be null or whitespaces.");
        }

        public static void ValidateApplicationTypeId(Guid typeId)
        {
            if (typeId == Guid.Empty)
                throw new InvalidOperationException("Application Type Id cannot be empty.");
        }

        public static void ValidateContentId(Guid contentId)
        {
            if (contentId == Guid.Empty)
                throw new InvalidOperationException("Content Id cannot be empty.");
        }

        public static void ValidateContentKey(string contentKey)
        {
            if (string.IsNullOrWhiteSpace(contentKey))
                throw new InvalidOperationException("Content Key cannot be null or whitespaces.");
        }
    }
}
