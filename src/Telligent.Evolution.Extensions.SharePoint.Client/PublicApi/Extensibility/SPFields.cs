using System;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public interface IFields : ICacheable
    {
        Microsoft.SharePoint.Client.Field Get(Guid listId, Guid fieldId);
    }

    public class Fields : IFields
    {
        private readonly IFieldsService fields;
        private readonly IListDataService lists;
        private readonly ICacheService cacheService;

        internal Fields() : this(ServiceLocator.Get<IFieldsService>(), ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<ICacheService>()) { }
        internal Fields(IFieldsService fields, IListDataService lists, ICacheService cacheService)
        {
            this.fields = fields;
            this.lists = lists;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        public Microsoft.SharePoint.Client.Field Get(Guid listId, Guid fieldId)
        {
            var cacheId = string.Concat("SPFields:", listId.ToString("N"), fieldId.ToString("N"));
            var field = (Microsoft.SharePoint.Client.Field)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (field == null)
            {
                var listBase = lists.Get(listId);
                if (listBase != null)
                {
                    field = fields.Get(listBase.SPWebUrl, listBase.Id, fieldId);
                    cacheService.Put(cacheId, field, CacheScope.Context | CacheScope.Process, new[] { string.Empty }, CacheTimeOut);
                }
            }
            return field;
        }
    }
}
