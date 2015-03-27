using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using SP = Microsoft.SharePoint.Client;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPListCreateOptions
    {
        public SPListCreateOptions(int groupId)
        {
            GroupId = groupId;
        }

        public int GroupId { get; private set; }
        public string Description { get; set; }
        public string SPWebUrl { get; set; }
        public string Title { get; set; }
        public string Template { get; set; }
    }

    public class SPListGetOptions
    {
        public SPListGetOptions(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public int GroupId { get; set; }
        public string Url { get; set; }
    }

    public class SPListCollectionOptions
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string Filter { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
    }

    public class ListUpdateOptions
    {
        public ListUpdateOptions(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public Guid? DefaultViewId { get; set; }
    }

    public interface ILists : ICacheable
    {
        ListEvents Events { get; }
        void Add(int groupId, Guid id, string url);
        void Update(ListUpdateOptions options);
        SPList Create(SPListCreateOptions options);
        SPList Get(SPListGetOptions options);
        SPList Get(Guid id);
        PagedList<SPList> List(int groupId, SPListCollectionOptions options);
        void Delete(SPList list, bool deleteLibrary);
        bool CanEdit(Guid listId);
    }

    public class Lists : ILists
    {
        private readonly IListService listService;
        private readonly ICacheService cacheService;

        public Lists()
            : this(ServiceLocator.Get<IListService>(), ServiceLocator.Get<ICacheService>())
        {
        }

        internal Lists(IListService listService, ICacheService cacheService)
        {
            this.listService = listService;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        private readonly ListEvents events = new ListEvents();
        public ListEvents Events
        {
            get { return events; }
        }

        public void Add(int groupId, Guid id, string url)
        {
            var list = new ListBase(groupId, id, ListApplicationType.Id, url);
            listService.Add(list);

            var group = TEApi.Groups.Get(new Extensibility.Api.Version1.GroupsGetOptions { Id = groupId });
            SecurityService.RecalculatePermissions(list.Id, ListApplicationType.Id, group.ApplicationId);

            ExpireTags(groupId);
        }

        public void Update(ListUpdateOptions options)
        {
            listService.Update(options);

            var list = listService.Get(new ListGetQuery(options.Id, LibraryApplicationType.Id)
            {
                Url = options.Url
            });
            ExpireTags(list.GroupId);
        }

        public SPList Create(SPListCreateOptions options)
        {
            try
            {
                Events.OnBeforeCreate(new SPList { SPWebUrl = options.SPWebUrl, Title = options.Title, Description = options.Description });
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Lists.Events.OnBeforeCreate() method GroupId: {1}. The exception message is: {2}", ex.GetType(), options.GroupId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            var template = SP.ListTemplateType.GenericList;
            if (!string.IsNullOrEmpty(options.Template))
            {
                Enum.TryParse(options.Template, out template);
            }

            var list = listService.Create((int)template, options);
            ExpireTags(options.GroupId);

            var group = TEApi.Groups.Get(new Extensibility.Api.Version1.GroupsGetOptions { Id = list.GroupId });
            SecurityService.RecalculatePermissions(list.Id, ListApplicationType.Id, group.ApplicationId);

            if (!list.HasErrors())
            {
                cacheService.Put(CacheKey(list.Id), list, CacheScope.Context | CacheScope.Process, new[] { Tag(list.GroupId) }, CacheTimeOut);
                try
                {
                    Events.OnAfterCreate(list);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the PublicApi.Lists.Events.OnAfterCreate() method GroupId: {1} ListId: {2} SPWebUrl: {3}. The exception message is: {4}", ex.GetType(), options.GroupId, list.Id, list.SPWebUrl, ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }

            return list;
        }

        public SPList Get(SPListGetOptions options)
        {
            var list = (SPList)cacheService.Get(CacheKey(options.Id), CacheScope.Context | CacheScope.Process);
            if (list == null)
            {
                list = listService.Get(options);
                if (list != null)
                {
                    cacheService.Put(CacheKey(list.Id), list, CacheScope.Context | CacheScope.Process, new[] { Tag(list.GroupId) }, CacheTimeOut);
                }
            }
            return list;
        }

        public SPList Get(Guid applicationId)
        {
            var list = (SPList)cacheService.Get(CacheKey(applicationId), CacheScope.Context | CacheScope.Process);
            if (list == null)
            {
                list = listService.Get(new SPListGetOptions(applicationId));
                if (list != null)
                {
                    cacheService.Put(CacheKey(list.Id), list, CacheScope.Context | CacheScope.Process, new[] { Tag(list.GroupId) }, CacheTimeOut);
                }
            }
            return list;
        }

        public PagedList<SPList> List(int groupId, SPListCollectionOptions options)
        {
            var lists = (List<SPList>)cacheService.Get(CacheKey(groupId), CacheScope.Context | CacheScope.Process);
            if (lists == null)
            {
                lists = listService.List(groupId, options);
                cacheService.Put(CacheKey(groupId), lists, CacheScope.Context | CacheScope.Process, new[] { Tag(groupId) }, CacheTimeOut);
            }

            if (!String.IsNullOrEmpty(options.Filter))
            {
                lists = lists.Where(list => list.Title.Contains(options.Filter, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }

            return new PagedList<SPList>(lists.Order(options.SortBy, options.SortOrder).Skip(options.PageSize * options.PageIndex).Take(options.PageSize))
                {
                    PageSize = options.PageSize,
                    PageIndex = options.PageIndex,
                    TotalCount = lists.Count
                };
        }

        public void Delete(SPList list, bool delete)
        {
            if (list == null)
                throw new NullReferenceException("List cannot be null.");

            if (list.Id == Guid.Empty)
                throw new InvalidOperationException("Id cannot be empty.");

            if (string.IsNullOrEmpty(list.SPWebUrl))
                throw new InvalidOperationException("SPWebUrl cannot be empty.");

            try
            {
                Events.OnBeforeDelete(list);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Lists.Events.OnBeforeDelete() method GroupId: {1} ListId: {2} SPWebUrl: {3} Delete: {4}. The exception message is: {5}", ex.GetType(), list.GroupId, list.Id, list.SPWebUrl, delete, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            listService.Delete(list.Id, delete);
            ExpireTags(list.GroupId);

            try
            {
                Events.OnAfterDelete(list);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Lists.Events.OnAfterDelete() method GroupId: {1} ListId: {2} SPWebUrl: {3} Delete: {4}. The exception message is: {5}", ex.GetType(), list.GroupId, list.Id, list.SPWebUrl, delete, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
        }

        public bool CanEdit(Guid listId)
        {
            var canEdit = (bool?)cacheService.Get(CanEditCacheKey(listId), CacheScope.Context | CacheScope.Process);
            if (canEdit == null)
            {
                canEdit = listService.CanEdit(listId);
                cacheService.Put(CanEditCacheKey(listId), canEdit, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
            }
            return (bool)canEdit;
        }

        #region Cache-related methods

        private void ExpireTags(int? groupId)
        {
            cacheService.RemoveByTags(new[] { Tag(groupId) }, CacheScope.Context | CacheScope.Process);
        }

        private static string CacheKey(Guid id)
        {
            return string.Concat("SharePoint_List:", id.ToString("N"));
        }

        private static string CacheKey(int groupId)
        {
            return string.Concat("SharePoint_Lists:", groupId, ":", ListApplicationType.Id.ToString("N"));
        }

        private static string Tag(int? groupId)
        {
            return string.Concat("SharePoint_List_TAG:", groupId);
        }

        private static string CanEditCacheKey(Guid applicationId)
        {
            return string.Concat("SharePoint_CanEditList:", applicationId.ToString("N"));
        }

        #endregion
    }
}
