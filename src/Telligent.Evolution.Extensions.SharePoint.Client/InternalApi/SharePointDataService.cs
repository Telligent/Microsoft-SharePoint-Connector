using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal static class SharePointDataService
    {
        internal static string ConnectionString { get; set; }

        internal static bool IsConnectionStringValid()
        {
            bool isValid = false;

            if (!string.IsNullOrEmpty(ConnectionString))
            {
                try
                {
                    using (var connection = GetSqlConnection())
                    {
                        using (var command = new SqlCommand("SELECT IS_MEMBER('db_owner') As IsOwner", connection))
                        {
                            connection.Open();
                            using (var reader = command.ExecuteReader())
                            {
                                isValid = reader.Read() && Convert.ToBoolean(reader["IsOwner"]);
                            }
                            connection.Close();
                        }
                    }
                }
                catch
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        internal static void Install()
        {
            using (var connection = GetSqlConnection())
            {
                connection.Open();
                foreach (string statement in GetStatementsFromSqlBatch(EmbeddedResources.GetString("Telligent.Evolution.Extensions.SharePoint.Client.Resources.Sql.install.sql")))
                {
                    using (var command = new SqlCommand(statement, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                connection.Close();
            }
        }

        internal static void UnInstall()
        {
            using (var connection = GetSqlConnection())
            {
                connection.Open();
                foreach (string statement in GetStatementsFromSqlBatch(EmbeddedResources.GetString("Telligent.Evolution.Extensions.SharePoint.Client.Resources.Sql.uninstall.sql")))
                {
                    using (var command = new SqlCommand(statement, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                connection.Close();
            }
        }

        #region Helpers

        private static IEnumerable<string> GetStatementsFromSqlBatch(string sqlBatch)
        {
            // This isn't as reliable as the SQL Server SDK, but works for most SQL batches and prevents another assembly reference
            foreach (string statement in Regex.Split(sqlBatch, @"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline))
            {
                string sanitizedStatement = Regex.Replace(statement, @"(?:^SET\s+.*?$|\/\*.*?\*\/|--.*?$)", "\r\n", RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();
                if (sanitizedStatement.Length > 0)
                    yield return sanitizedStatement;
            }
        }

        private static SqlConnection GetSqlConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        #endregion
    }
}
