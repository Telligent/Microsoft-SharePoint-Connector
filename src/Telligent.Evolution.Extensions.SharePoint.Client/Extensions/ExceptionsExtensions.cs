using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    internal static class ExceptionsExtensions
    {
        public static bool IsUnauthorizedAccessException(this Exception ex)
        {
            return ex.Message == "The remote server returned an error: (401) Unauthorized.";
        }
    }
}
