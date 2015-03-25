using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class ListQuery
    {
        public static implicit operator ListQuery(LibraryListOptions options)
        {
            return new ListQuery(LibraryApplicationType.Id);
        }

        public static implicit operator ListQuery(SPListCollectionOptions options)
        {
            return new ListQuery(ListApplicationType.Id);
        }

        public ListQuery(Guid typeId)
        {
            TypeId = typeId;
        }

        public Guid TypeId { get; set; }
    }

    internal class ListGetQuery
    {
        private const int RootGroupId = -1;

        public static implicit operator ListGetQuery(LibraryGetOptions options)
        {
            return new ListGetQuery(options.Id, LibraryApplicationType.Id)
            {
                Url = options.Url,
                GroupId = options.GroupId
            };
        }

        public static implicit operator ListGetQuery(SPListGetOptions options)
        {
            return new ListGetQuery(options.Id, ListApplicationType.Id)
            {
                Url = options.Url,
                GroupId = options.GroupId
            };
        }

        public ListGetQuery(Guid id, Guid typeId)
        {
            Id = id;
            TypeId = typeId;
            GroupId = RootGroupId;
        }

        public Guid Id { get; private set; }
        public int GroupId { get; set; }
        public Guid TypeId { get; set; }
        public string Url { get; set; }
    }

    internal class ListUpdateQuery
    {
        public static implicit operator ListUpdateQuery(LibraryUpdateOptions options)
        {
            return new ListUpdateQuery(options.Id)
            {
                SPWebUrl = options.Url,
                Title = options.Title,
                Description = options.Description,
                DefaultViewId = options.DefaultViewId
            };
        }

        public static implicit operator ListUpdateQuery(ListUpdateOptions options)
        {
            return new ListUpdateQuery(options.Id)
            {
                SPWebUrl = options.Url,
                Title = options.Title,
                Description = options.Description,
                DefaultViewId = options.DefaultViewId
            };
        }

        public ListUpdateQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public string SPWebUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid? DefaultViewId { get; set; }
    }

    internal class ListCreateQuery
    {
        public static implicit operator ListCreateQuery(LibraryCreateOptions options)
        {
            return new ListCreateQuery
            {
                TypeId = LibraryApplicationType.Id,
                GroupId = options.GroupId,
                Description = options.Description,
                SPWebUrl = options.SPWebUrl,
                Title = options.Title
            };
        }

        public static implicit operator ListCreateQuery(SPListCreateOptions options)
        {
            return new ListCreateQuery
            {
                TypeId = ListApplicationType.Id,
                GroupId = options.GroupId,
                Description = options.Description,
                SPWebUrl = options.SPWebUrl,
                Title = options.Title
            };
        }

        public Guid TypeId { get; set; }
        public int GroupId { get; set; }
        public string Description { get; set; }
        public string SPWebUrl { get; set; }
        public string Title { get; set; }
    }

    internal interface IListService : IIndexable<SPList>
    {
        void Add(ListBase list);
        SPList Create(int templateType, ListCreateQuery options);
        SPList Get(ListGetQuery options);
        void Update(ListUpdateQuery options);
        List<SPList> List(int groupId, ListQuery options);
        void Delete(Guid id, bool deleteList);
        bool CanEdit(Guid listId);
    }

    internal class SPListService : IListService
    {
        private readonly ICredentialsManager credentials;
        private readonly IListDataService listDataService;
        private readonly IListItemService listItemService;
        private readonly IListItemDataService listItemDataService;

        public static Expression<Func<ListCollection, object>> ListInstanceQuery
        {
            get
            {
                return lists => lists.Include(
                    list => list.BaseType,
                    list => list.Created,
                    list => list.DefaultViewUrl,
                    list => list.Description,
                    list => list.EnableVersioning,
                    list => list.Fields,
                    list => list.Id,
                    list => list.ItemCount,
                    list => list.LastItemModifiedDate,
                    list => list.ParentWebUrl,
                    list => list.RootFolder,
                    list => list.Title,
                    list => list.Views);
            }
        }
        public static Expression<Func<ListCollection, object>> NoHiddenFieldsListInstanceQuery
        {
            get
            {
                return lists => lists.Include(
                    list => list.BaseType,
                    list => list.Created,
                    list => list.DefaultViewUrl,
                    list => list.Description,
                    list => list.EnableVersioning,
                    list => list.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"),
                    list => list.Id,
                    list => list.ItemCount,
                    list => list.LastItemModifiedDate,
                    list => list.ParentWebUrl,
                    list => list.RootFolder,
                    list => list.Title,
                    list => list.Views);
            }
        }
        public static Expression<Func<List, object>>[] InstanceQuery
        {
            get
            {
                return new Expression<Func<List, object>>[]{
                    list => list.BaseType,
                    list => list.Created,
                    list => list.DefaultViewUrl,
                    list => list.Description,
                    list => list.EnableVersioning,
                    list => list.Fields,
                    list => list.Id,
                    list => list.ItemCount,
                    list => list.LastItemModifiedDate,
                    list => list.ParentWeb.Id,
                    list => list.ParentWebUrl,
                    list => list.RootFolder,
                    list => list.Title,
                    list => list.Views
                };
            }
        }
        public static Expression<Func<List, object>>[] NoHiddenFieldsInstanceQuery
        {
            get
            {
                return new Expression<Func<List, object>>[]{
                    list => list.BaseType,
                    list => list.Created,
                    list => list.DefaultViewUrl,
                    list => list.Description,
                    list => list.EnableVersioning,
                    list => list.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"),
                    list => list.Id,
                    list => list.ItemCount,
                    list => list.LastItemModifiedDate,
                    list => list.ParentWeb.Id,
                    list => list.ParentWebUrl,
                    list => list.RootFolder,
                    list => list.Title,
                    list => list.Views
                };
            }
        }

        public SPListService()
            : this(ServiceLocator.Get<ICredentialsManager>(), ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<IListItemService>(), ServiceLocator.Get<IListItemDataService>())
        {
        }

        public SPListService(ICredentialsManager credentials, IListDataService listDataService, IListItemService listItemService, IListItemDataService listItemDataService)
        {
            this.credentials = credentials;
            this.listDataService = listDataService;
            this.listItemDataService = listItemDataService;
            this.listItemService = listItemService;
        }

        #region IListService

        public void Add(ListBase list)
        {
            Validate(list);

            if (string.IsNullOrEmpty(list.ApplicationKey))
            {
                var splist = Get(new ListGetQuery(list.Id, list.TypeId)
                {
                    Url = list.SPWebUrl
                });
                list.ApplicationKey = splist.Title;
            }

            listDataService.AddUpdate(list);

            SyncListItems(list);
        }

        public void Update(ListUpdateQuery options)
        {
            if (options.Id == Guid.Empty) return;

            ListBase listBase = null;
            string spwebUrl = options.SPWebUrl;
            if (string.IsNullOrEmpty(spwebUrl))
            {
                listBase = listDataService.Get(options.Id);
                if (listBase == null) return;

                Validate(listBase);
                spwebUrl = listBase.SPWebUrl;
            }

            try
            {
                var hasSharePointUpdates = options.Title != null || options.Description != null;
                if (hasSharePointUpdates)
                {
                    using (var clientContext = new SPContext(spwebUrl, credentials.Get(spwebUrl)))
                    {
                        var splist = clientContext.Web.Lists.GetById(options.Id);
                        if (!string.IsNullOrEmpty(options.Title))
                        {
                            splist.Title = options.Title;
                        }
                        if (options.Description != null)
                        {
                            splist.Description = options.Description;
                        }
                        splist.Update();
                        clientContext.ExecuteQuery();

                        if (listBase != null)
                        {
                            listBase.ApplicationKey = splist.Title;
                        }
                    }
                }
                if (listBase != null)
                {
                    listBase.ViewId = options.DefaultViewId ?? Guid.Empty;
                    listDataService.AddUpdate(listBase);
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPListService.Update() method. The exception message is: {1}", ex.GetType(), ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        public SPList Create(int templateType, ListCreateQuery options)
        {
            Validate(options);

            SPList list;
            try
            {
                using (var clientContext = new SPContext(options.SPWebUrl, credentials.Get(options.SPWebUrl)))
                {
                    var site = clientContext.Site;
                    clientContext.Load(site, s => s.Id);

                    var listCreationInformation = new ListCreationInformation
                    {
                        Title = options.Title,
                        Description = options.Description,
                        TemplateType = templateType
                    };

                    var splist = clientContext.Web.Lists.Add(listCreationInformation);
                    clientContext.Load(splist, InstanceQuery);

                    clientContext.ExecuteQuery();

                    list = new SPList(splist, site.Id) { GroupId = options.GroupId };

                    Add(new ListBase(list.GroupId, list.Id, options.TypeId, options.SPWebUrl)
                    {
                        ApplicationKey = list.Title
                    });
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPListService.Create() method. The exception message is: {1}", ex.GetType(), ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }

            return list;
        }

        public SPList Get(ListGetQuery options)
        {
            var list = new SPList();
            if (options.Id == Guid.Empty) return list;

            ListBase listBase = null;

            string spwebUrl = options.Url;
            int groupId = options.GroupId;
            if (string.IsNullOrEmpty(spwebUrl))
            {
                try
                {
                    listBase = listDataService.Get(options.Id);
                    if (listBase == null) return null;

                    Validate(listBase);

                    spwebUrl = listBase.SPWebUrl;
                    groupId = listBase.GroupId;
                }
                catch (InvalidOperationException)
                {
                    return new SPList
                    {
                        ApplicationId = options.Id,
                        GroupId = options.GroupId,
                        SPWebUrl = options.Url
                    };
                }
            }

            try
            {
                using (var clientContext = new SPContext(spwebUrl, credentials.Get(spwebUrl)))
                {
                    var site = clientContext.Site;
                    clientContext.Load(site, s => s.Id);

                    var web = clientContext.Web;
                    clientContext.Load(web, w => w.Id);

                    var spList = clientContext.Web.Lists.GetById(options.Id);
                    clientContext.Load(spList, NoHiddenFieldsInstanceQuery);
                    clientContext.ExecuteQuery();

                    list = new SPList(spList, site.Id)
                    {
                        GroupId = groupId,
                        ViewId = listBase != null ? listBase.ViewId : Guid.Empty
                    };
                }
            }
            catch (ServerUnauthorizedAccessException)
            {
                return null;
            }
            catch (Exception ex)
            {
                if (ex.IsUnauthorizedAccessException()) return null;

                if (listBase != null && ex.Message.StartsWith("List does not exist."))
                {
                    Delete(listBase.Id, false);
                    list = new SPList { Id = listBase.Id, GroupId = listBase.GroupId, SPWebUrl = listBase.SPWebUrl };
                }
                else
                {
                    var message = string.Format("An exception of type {0} occurred in the InternalApi.SPListService.Get() method for ContentId: {1}. The exception message is: {2}",
                            ex.GetType(),
                            options.Id,
                            ex.Message);

                    SPLog.RoleOperationUnavailable(ex, message);

                    throw new SPInternalException(message, ex);
                }
            }

            return list;
        }

        public List<SPList> List(int groupId, ListQuery options)
        {
            var lists = new List<SPList>();
            var listBases = listDataService.List(groupId, options.TypeId);
            foreach (var listBase in listBases)
            {
                try
                {
                    var list = Get(new ListGetQuery(listBase.Id, listBase.TypeId));
                    if (list != null) lists.Add(list);
                }
                catch (SPInternalException) { }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the InternalApi.SPListService.List() method for GroupId: {1}. The exception message is: {2}", ex.GetType(), groupId, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);
                }
            }
            return lists;
        }

        public void Delete(Guid id, bool deleteList)
        {
            var list = listDataService.Get(id);
            if (list == null) return;

            Validate(list);

            listDataService.Delete(list.Id);
            if (deleteList)
            {
                try
                {
                    using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
                    {
                        var splist = clientContext.Web.Lists.GetById(list.Id);
                        splist.DeleteObject();
                        clientContext.ExecuteQuery();
                    }
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the InternalApi.SPListService.Delete() method for ApplicationId: {1}. The exception message is: {2}", ex.GetType(), id, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    throw new SPInternalException(message, ex);
                }
            }
        }

        public bool CanEdit(Guid listId)
        {
            var listBase = listDataService.Get(listId);
            if (listBase == null) return false;

            Validate(listBase);

            try
            {
                bool? canEdit;
                using (var spcontext = new SPContext(listBase.SPWebUrl, credentials.Get(listBase.SPWebUrl)))
                {
                    var splist = spcontext.Web.Lists.GetById(listId);
                    spcontext.Load(splist, l => l.EffectiveBasePermissions);
                    spcontext.ExecuteQuery();
                    canEdit = splist.EffectiveBasePermissions.Has(PermissionKind.EditListItems);
                }
                return canEdit.Value;
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPListService.CanEdit() method for ApplicationId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        #endregion

        #region IIndexable

        public PagedList<SPList> ListItemsToReindex(Guid typeId, int batchSize, string[] viewFields = null)
        {
            var lists = listDataService.ListsToReindex(typeId, batchSize);
            var spLists = new PagedList<SPList>
            {
                PageSize = lists.PageSize,
                PageIndex = lists.PageIndex,
                TotalCount = lists.TotalCount
            };

            foreach (var list in lists)
            {
                try
                {
                    var splist = Get(new ListGetQuery(list.Id, typeId));
                    if (splist != null) spLists.Add(splist);
                }
                catch (SPInternalException) { }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the InternalApi.SPListService.ListsToReindex() method for ApplicationTypeId: {1} BatchSize: {2}. The exception message is: {3}", ex.GetType(), typeId, batchSize, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);
                }
            }

            return spLists;
        }

        public void UpdateIndexingStatus(Guid[] contentIds, bool isIndexed)
        {
            try
            {
                listDataService.UpdateIndexingStatus(contentIds, isIndexed);
            }
            catch (Exception ex)
            {
                var ids = contentIds != null && contentIds.Length > 0 ? string.Join(", ", contentIds) : string.Empty;
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPListService.UpdateIndexingStatus() method for ContentIds: {1} IsIndexed: {2}. The exception message is: {3}", ex.GetType(), ids, isIndexed, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);
            }
        }

        #endregion

        #region Validate Methods

        private void Validate(ListCreateQuery options)
        {
            if (options.GroupId <= 0)
                throw new InvalidOperationException("The group identified is invalid.");

            if (string.IsNullOrEmpty(options.SPWebUrl))
                throw new InvalidOperationException("Url cannot be empty.");

            if (string.IsNullOrEmpty(options.Title))
                throw new InvalidOperationException("Title cannot be empty.");
        }

        private void Validate(ListBase list)
        {
            if (list.GroupId <= 0)
                throw new InvalidOperationException("The group identified is invalid.");

            if (list.Id == Guid.Empty)
                throw new InvalidOperationException("Id cannot be empty.");

            if (string.IsNullOrEmpty(list.SPWebUrl))
                throw new InvalidOperationException("Url cannot be empty.");
        }

        #endregion

        private void SyncListItems(ListBase list)
        {
            var itemsFromSharePoint = listItemService.GetItemsFromSharePoint(list);
            if (itemsFromSharePoint.Any())
            {
                listItemDataService.AddUpdate(itemsFromSharePoint);
            }
        }
    }
}
