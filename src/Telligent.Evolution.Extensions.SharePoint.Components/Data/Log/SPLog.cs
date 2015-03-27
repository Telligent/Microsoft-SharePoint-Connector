using System;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Data.Log
{
    public class SPLog
    {
        internal static void Log(Exception exception, CSExceptionType exceptionType, string msg, params Object[] args)
        {
            try
            {
                string logMsg = string.Format(msg, args);
                var csEx = new CSException(exceptionType, logMsg, exception);
                csEx.Log();
            }
            catch (Exception ex)
            {
                var csEx = new CSException(exceptionType, string.Format("Error logging error: {0}", msg), ex);
                csEx.Log();
            }
        }

        public static void AccessDenied(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.AccessDenied, msg, args);
        }

        public static void BackgroundJobError(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.BackgroundJobError, msg, args);
        }

        public static void DataProvider(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.DataProvider, msg, args);
        }

        public static void FileNotFound(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.FileNotFound, msg, args);
        }

        public static void ResourceNotFound(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.ResourceNotFound, msg, args);
        }

        public static void RoleOperationUnavailable(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.RoleOperationUnavailable, msg, args);
        }

        public static void SiteSettingsInvalidXML(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.SiteSettingsInvalidXML, msg, args);
        }

        public static void UserInvalidCredentials(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.UserInvalidCredentials, msg, args);
        }

        public static void UserNotFound(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.UserNotFound, msg, args);
        }

        public static void UnKnownError(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.UnknownError, msg, args);
        }

        public static void UserProfileUpdated(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.UserProfileUpdated, msg, args);
        }

        public static void Event(string msg)
        {
            PublicApi.Eventlogs.Write(msg, new EventLogEntryWriteOptions { Category = "SharePoint", EventId = 2010, EventType = "Warning" });
        }

        public static void Info(string msg)
        {
            PublicApi.Eventlogs.Write(msg, new EventLogEntryWriteOptions { Category = "SharePoint", EventId = 2010, EventType = "Information" });
        }
    }
}
