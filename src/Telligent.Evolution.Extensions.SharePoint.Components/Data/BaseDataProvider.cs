using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Data
{
    public class BaseDataProvider
    {
        protected static SqlConnection GetConnection()
        {
            return new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString);
        }

        private static SqlCommand CreateCommand(string spName, SqlConnection connection, params SqlParameter[] param)
        {
            SqlCommand cmd = new SqlCommand(spName, connection);
            cmd.CommandType = CommandType.StoredProcedure;
            if (param != null)
                cmd.Parameters.AddRange(param);
            return cmd;
        }

        protected static SqlDataReader ExecuteReader(string spName, SqlConnection connection, params SqlParameter[] param)
        {
            connection.Open();
            SqlCommand cmd = CreateCommand(spName, connection, param);
            return cmd.ExecuteReader();
        }

        protected static object ExecuteScalar(string spName, SqlConnection connection, params SqlParameter[] param)
        {
            try
            {
                connection.Open();
                SqlCommand cmd = CreateCommand(spName, connection, param);
                return cmd.ExecuteScalar();
            }
            finally
            {
                connection.Close();
            }
        }

        protected static void ExecuteNonQuery(string spName, SqlConnection connection, params SqlParameter[] param)
        {
            try
            {
                connection.Open();
                SqlCommand cmd = CreateCommand(spName, connection, param);
                cmd.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        protected static SqlParameter PrepareNullParameter(string parameterName, object value)
        {
            if (value == null)
                return new SqlParameter(parameterName, DBNull.Value);
            return new SqlParameter(parameterName, value);
        }

        protected static List<T> GetList<T>(string spName, SqlConnection connection, Converter<SqlDataReader, List<T>> fillList, params SqlParameter[] param)
        {
            List<T> result = new List<T>();
            try
            {
                connection.Open();
                SqlCommand cmd = CreateCommand(spName, connection, param);
                SqlDataReader dr = cmd.ExecuteReader();
                result = fillList(dr);
            }
            finally
            {
                connection.Close();
            }
            return result;
        }

        protected static T GetOne<T>(string spName, SqlConnection connection, Converter<SqlDataReader, List<T>> fillList, params SqlParameter[] param)
        {
            T result = default(T);
            try
            {
                connection.Open();
                SqlCommand cmd = CreateCommand(spName, connection, param);
                SqlDataReader dr = cmd.ExecuteReader();
                List<T> list = fillList(dr);
                if (list.Count > 0)
                    result = (T)list[0];
            }
            finally
            {
                connection.Close();
            }
            return result;
        }
    }
}
