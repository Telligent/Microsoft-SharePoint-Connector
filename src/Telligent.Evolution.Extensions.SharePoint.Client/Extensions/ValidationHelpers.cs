using System;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    internal static class ValidationHelpers
    {
        public static ItemBase Validate(this ItemBase item)
        {
            if (item.ApplicationId == Guid.Empty)
                throw new InvalidOperationException("Invalid List Id (ApplicationId).");

            if (item.UniqueId == Guid.Empty)
                throw new InvalidOperationException("Invalid Item UniqueId (ContentId).");

            if (item.Id <= 0)
                throw new InvalidOperationException("Invaid Item Id.");

            return item;
        }

        public static ListBase Validate(this ListBase list)
        {
            if (list.Id == Guid.Empty)
                throw new InvalidOperationException("Invalid List Id (ApplicationId).");

            if (list.TypeId == Guid.Empty)
                throw new InvalidOperationException("Invalid Application Type Id.");

            if (list.GroupId <= 0)
                throw new InvalidOperationException("Invalid Telligent Evolution Group Id.");

            if (string.IsNullOrWhiteSpace(list.SPWebUrl))
                throw new InvalidOperationException("Invalid SharePoint Web URL.");

            return list;
        }
    }
}
