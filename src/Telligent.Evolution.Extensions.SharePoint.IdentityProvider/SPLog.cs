using System;
using Telligent.Evolution.Components;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider
{
    public class SPLog
    {
        internal static void Log(Exception exception, CSExceptionType exceptionType, string msg, params Object[] args)
        {
            string logMsg = string.Format(msg, args);
            throw new Exception(logMsg);
        }

        public static void UserInvalidCredentials(Exception exception, string msg, params Object[] args)
        {
            Log(exception, CSExceptionType.UserInvalidCredentials, msg, args);
        }

        public static void Event(string msg)
        {
            EventLogs.Warn(msg, "SharePoint", 3681);
        }
    }
}
