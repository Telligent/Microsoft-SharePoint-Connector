using System;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public interface IUserProfiles : ICacheable
    {
        SPUser Get(Guid listId, int id);
    }

    public class UserProfiles : IUserProfiles
    {
        private readonly IUserProfileService userProfiles;
        private readonly IListDataService listDataService;
        private readonly ICacheService cacheService;

        public UserProfiles() : this(ServiceLocator.Get<IUserProfileService>(), ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<ICacheService>()) { }
        internal UserProfiles(IUserProfileService userProfiles, IListDataService listDataService, ICacheService cacheService)
        {
            this.cacheService = cacheService;
            this.listDataService = listDataService;
            this.userProfiles = userProfiles;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        public SPUser Get(Guid listId, int id)
        {
            var cacheId = string.Concat("UserProfiles.Get:", listId.ToString("N"), id);
            var userProfile = (SPUser)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (userProfile == null)
            {
                userProfile = userProfiles.Get(GetUrl(listId), id);
                cacheService.Put(cacheId, userProfile, CacheScope.Context | CacheScope.Process, null, CacheTimeOut);
            }
            return userProfile;
        }

        private string GetUrl(Guid listId)
        {
            var list = listDataService.Get(listId);
            if (list != null)
                return list.SPWebUrl;
            return null;
        }
    }
}
