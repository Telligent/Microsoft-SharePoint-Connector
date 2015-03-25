using System;
using Telligent.Evolution.Extensibility.Caching.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface ICacheService
    {
        object Get(string key, CacheScope scope);

        void Put(string key, object data, CacheScope scope, string[] tags, TimeSpan timeOut);

        void Put(string key, object data, CacheScope scope, string[] tags);

        void Remove(string key, CacheScope scope);

        void RemoveByTags(string[] tags, CacheScope scope);
    }

    internal class SPCacheService : ICacheService
    {
        public object Get(string key, CacheScope scope)
        {
            return CacheService.Get(string.Format("{0}:{1}", key, SPCoreService.UserId), scope);
        }

        public void Put(string key, object data, CacheScope scope, string[] tags, TimeSpan timeOut)
        {
            CacheService.Put(string.Format("{0}:{1}", key, SPCoreService.UserId), data, scope, tags, timeOut);
        }

        public void Put(string key, object data, CacheScope scope, string[] tags)
        {
            CacheService.Put(string.Format("{0}:{1}", key, SPCoreService.UserId), data, scope, tags);
        }

        public void Remove(string key, CacheScope scope)
        {
            CacheService.Remove(string.Format("{0}:{1}", key, SPCoreService.UserId), scope);
        }

        public void RemoveByTags(string[] tags, CacheScope scope)
        {
            CacheService.RemoveByTags(tags, scope);
        }
    }
}
