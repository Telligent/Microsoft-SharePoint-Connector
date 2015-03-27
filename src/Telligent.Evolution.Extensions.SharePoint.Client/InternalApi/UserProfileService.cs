using System;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IUserProfileService
    {
        SPUser Get(string url, int lookupId);
    }

    internal class UserProfileService : IUserProfileService
    {
        private readonly ICredentialsManager credentials;

        public UserProfileService() : this(ServiceLocator.Get<ICredentialsManager>()) { }
        public UserProfileService(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public SPUser Get(string url, int lookupId)
        {
            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {
                try
                {
                    var userProfile = clientContext.Web.SiteUserInfoList.GetItemById(lookupId);
                    clientContext.Load(userProfile, SPUser.InstanceQuery);
                    clientContext.ExecuteQuery();
                    return new SPUser(userProfile);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the InternalApi.UserProfileService.Get() method URL: {1}, LookupId: {2}. The exception message is: {4}", ex.GetType(), url, lookupId, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);
                }
            }
            return null;
        }
    }
}
