using System;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class SPCoreService
    {
        internal static Guid? ApplicationTypeId
        {
            get
            {
                return TEApi.Url.CurrentContext.ApplicationTypeId;
            }
        }

        internal static int UserId
        {
            get
            {
                return TEApi.Users.AccessingUser.Id ?? -1;
            }
        }

        internal static IContextService Context
        {
            get
            {
                return ServiceLocator.Get<IContextService>();
            }
        }
    }
}
