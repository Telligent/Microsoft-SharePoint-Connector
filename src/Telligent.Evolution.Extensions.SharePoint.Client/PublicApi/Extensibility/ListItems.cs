using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPListItemCreateOptions
    {
        public string FolderUrl { get; set; }
        public string Name { get; set; }
        public bool? IsFolder { get; set; }
        public string Url { get; set; }
    }

    public class SPListItemGetOptions
    {
        public SPListItemGetOptions(Guid uniqueId)
        {
            UniqueId = uniqueId;
        }

        public SPListItemGetOptions(int itemId)
        {
            ItemId = itemId;
        }

        public int? ItemId { get; private set; }
        public Guid UniqueId { get; private set; }
        public string Url { get; set; }
    }

    public class SPListItemUpdateOptions : SPListItemGetOptions
    {
        public SPListItemUpdateOptions(int itemId, IDictionary fields)
            : base(itemId)
        {
            Fields = fields;
        }

        public SPListItemUpdateOptions(Guid uniqueId, IDictionary fields)
            : base(uniqueId)
        {
            Fields = fields;
        }

        public IDictionary Fields { get; private set; }
    }

    public class SPListItemDeleteOptions
    {
        public SPListItemDeleteOptions(IEnumerable<Guid> contentIds)
        {
            ItemIds = new List<int>();
            ContentIds = new List<Guid>(contentIds);
        }

        public SPListItemDeleteOptions(IEnumerable<int> ids)
        {
            ItemIds = new List<int>(ids);
            ContentIds = new List<Guid>();
        }

        public SPListItemDeleteOptions(IEnumerable<int> ids, IEnumerable<Guid> contentIds)
        {
            ContentIds = new List<Guid>(contentIds);
            ItemIds = new List<int>(ids);
        }

        public List<int> ItemIds { get; private set; }
        public List<Guid> ContentIds { get; private set; }
        public string Url { get; set; }
    }

    public class SPListItemCollectionOptions
    {
        public SPListItemCollectionOptions()
        {
            PageSize = 20;
        }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; }
        public SortOrder SortOrder { get; set; }
        public string Url { get; set; }
        public List<string> ViewFields { get; set; }
        public string ViewQuery { get; set; }
    }

    public interface IListItems : ICacheable
    {
        ListItemEvents Events { get; }
        SPListItem Create(Guid listId, SPListItemCreateOptions options);
        SPListItem Get(Guid listId, SPListItemGetOptions options);
        PagedList<SPListItem> List(Guid listId, SPListItemCollectionOptions options = null);
        SPListItem Update(Guid listId, SPListItemUpdateOptions options);
        void Delete(Guid listId, SPListItemDeleteOptions options);
        bool CanEdit(Guid listId, SPListItemGetOptions options);
        void ExpireTags(Guid applicationId);
    }

    public class ListItems : IListItems
    {
        private const int DefaultPageSize = 20;

        internal static readonly string[] ViewFields = { "UniqueId", "Title", "Created", "Modified", "DocIcon", "ContentTypeId", "Author", "Editor" };

        private readonly IListItemService listItemService;
        private readonly ICacheService cacheService;

        internal ListItems() : this(ServiceLocator.Get<IListItemService>(), ServiceLocator.Get<ICacheService>()) { }
        internal ListItems(IListItemService listItemService, ICacheService cacheService)
        {
            this.listItemService = listItemService;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        private readonly ListItemEvents events = new ListItemEvents();
        public ListItemEvents Events
        {
            get { return events; }
        }

        public SPListItem Create(Guid listId, SPListItemCreateOptions options)
        {
            try
            {
                Events.OnBeforeCreate(new SPListItem { Url = options.Url, DisplayName = options.Name });
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the PublicApi.ListItems.Events.OnBeforeCreate() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            var listItem = listItemService.Create(listId, options);
            ExpireTags(listId);

            var newItem = Get(listId, new SPListItemGetOptions(listItem.ContentId));
            if (newItem == null) return null;

            try
            {
                Events.OnAfterCreate(newItem);
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the PublicApi.ListItems.Events.OnAfterCreate() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            return newItem;
        }

        public SPListItem Get(Guid listId, SPListItemGetOptions options)
        {
            var cacheId = GetCacheId(listId, options);
            var listItem = (SPListItem)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (listItem != null) return listItem;

            listItem = listItemService.Get(listId, options);
            cacheService.Put(cacheId, listItem, CacheScope.Context | CacheScope.Process, new[] { Tag(listId) }, CacheTimeOut);
            return listItem;
        }

        public PagedList<SPListItem> List(Guid listId, SPListItemCollectionOptions options = null)
        {
            if (options == null)
            {
                options = new SPListItemCollectionOptions { PageSize = DefaultPageSize };
            }

            var cacheId = GetCacheId(listId, options);
            var listItems = (PagedList<SPListItem>)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (listItems == null)
            {
                listItems = listItemService.List(listId, options);
                cacheService.Put(cacheId, listItems, CacheScope.Context | CacheScope.Process, new[] { Tag(listId) }, CacheTimeOut);
            }
            return listItems;
        }

        public SPListItem Update(Guid listId, SPListItemUpdateOptions options)
        {
            var listItemId = options.ItemId.HasValue ? options.ItemId.Value.ToString(CultureInfo.InvariantCulture) : options.UniqueId.ToString("N");
            var beforeUpdateListItem = Get(listId, options);
            if (beforeUpdateListItem == null) return null;

            try
            {
                Events.OnBeforeUpdate(beforeUpdateListItem);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.ListItems.Events.OnBeforeUpdate() method ListId: {1} ListItemId: {2}. The exception message is: {3}", ex.GetType(), listId, listItemId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            var afterUpdateListItem = listItemService.Update(listId, options);
            ExpireTags(afterUpdateListItem.ListId);

            var listItem = Get(listId, options);
            if (listItem != null)
            {
                // TODO:

                try
                {
                    Events.OnAfterUpdate(listItem);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the PublicApi.ListItems.Events.OnAfterUpdate() method ListId: {1} ListItemId: {2}. The exception message is: {3}", ex.GetType(), listId, listItemId, ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }
            return listItem;
        }

        public void Delete(Guid listId, SPListItemDeleteOptions options)
        {
            var itemsToDelete = new List<SPListItem>();
            if (options.ItemIds != null && options.ItemIds.Count > 0)
            {
                itemsToDelete.AddRange(options.ItemIds.Select(id => Get(listId, new SPListItemGetOptions(id) { Url = options.Url })));
            }
            if (options.ContentIds != null && options.ContentIds.Count > 0)
            {
                itemsToDelete.AddRange(options.ContentIds.Select(id => Get(listId, new SPListItemGetOptions(id) { Url = options.Url })));
            }

            var contentIds = itemsToDelete.Select(item => item.ContentId).ToList();

            try
            {
                itemsToDelete.ForEach(Events.OnBeforeDelete);
            }
            catch (Exception ex)
            {
                var ids = string.Join(", ", contentIds);
                string message = string.Format("An exception of type {0} occurred in the PublicApi.ListItems.Events.OnBeforeDelete() method ContentIds: {1}. The exception message is: {2}", ex.GetType(), ids, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            listItemService.Delete(listId, options);
            ExpireTags(listId);

            try
            {
                itemsToDelete.ForEach(Events.OnAfterDelete);
            }
            catch (Exception ex)
            {
                var ids = string.Join(", ", contentIds);
                string message = string.Format("An exception of type {0} occurred in the PublicApi.ListItems.Events.OnAfterDelete() method ContentIds: {1}. The exception message is: {2}", ex.GetType(), ids, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
        }

        public bool CanEdit(Guid listId, SPListItemGetOptions options)
        {
            var cacheId = CanEditCacheKey(listId, options);
            var canEdit = (bool?)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (canEdit == null)
            {
                canEdit = listItemService.CanEdit(listId, options);
                cacheService.Put(cacheId, canEdit, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
            }
            return (bool)canEdit;
        }

        #region Cache Methods

        private static string GetListItemId(SPListItemGetOptions options)
        {
            return options.ItemId.HasValue ? options.ItemId.Value.ToString(CultureInfo.InvariantCulture) : options.UniqueId.ToString("N");
        }

        private static string GetCacheId(Guid applicationId, SPListItemGetOptions options)
        {
            return string.Join(":",
                                new[]
                                {
                                    "ListItems.Get",
                                     applicationId.ToString("N"),
                                     GetListItemId(options)
                                });
        }

        private static string GetCacheId(Guid listId, SPListItemCollectionOptions options)
        {
            return string.Join(":", new[]
                                {
                                    "ListItems.List",
                                    listId.ToString("N"),
                                    options.PageSize.ToString(CultureInfo.InvariantCulture),
                                    options.PageIndex.ToString(CultureInfo.InvariantCulture),
                                    options.SortBy,
                                    options.SortOrder.ToString(),
                                    string.Join(":",options.ViewFields),
                                    options.ViewQuery
                                });
        }

        public void ExpireTags(Guid applicationId)
        {
            cacheService.RemoveByTags(new[] { Tag(applicationId), Documents.Tag(applicationId) }, CacheScope.Context | CacheScope.Process);
        }

        private static string CanEditCacheKey(Guid applicationId, SPListItemGetOptions options)
        {
            return string.Concat("ListItems.CanEdit:", applicationId.ToString("N"), ":", GetListItemId(options));
        }

        internal static string Tag(Guid applicationId)
        {
            return string.Concat("SharePoint_Item_TAG:", applicationId);
        }

        #endregion
    }
}
