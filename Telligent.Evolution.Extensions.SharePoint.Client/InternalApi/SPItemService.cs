using System.Text.RegularExpressions;
using Microsoft.SharePoint.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class DocumentCreateQuery
    {
        public static implicit operator DocumentCreateQuery(DocumentCreateOptions options)
        {
            if (options.Data != null && options.Data.Length > 0)
            {
                return new DocumentCreateQuery(options.FilePath, options.Data)
                {
                    Overwrite = options.Overwrite
                };
            }
            return new DocumentCreateQuery(options.FilePath, options.CFSFileStore, options.CFSFilePath, options.CFSFileName)
            {
                Overwrite = options.Overwrite
            };
        }

        public DocumentCreateQuery(string filePath, byte[] data)
        {
            if (data.Length == 0)
                throw new InvalidOperationException("File cannot be empty.");

            FilePath = filePath;
            Data = data;
        }

        public DocumentCreateQuery(string filePath, string cfsFileStore, string cfsFilePath, string cfsFileName)
        {
            if (string.IsNullOrEmpty(cfsFileStore))
                throw new InvalidOperationException("CFS file store cannot be empty.");

            if (string.IsNullOrEmpty(cfsFilePath))
                throw new InvalidOperationException("CFS file path cannot be empty.");

            if (string.IsNullOrEmpty(cfsFileName))
                throw new InvalidOperationException("CFS file name cannot be empty.");

            FilePath = filePath;
            CFSFileStore = cfsFileStore;
            CFSFilePath = cfsFilePath;
            CFSFileName = cfsFileName;
        }

        public string CFSFileStore { get; private set; }
        public string CFSFilePath { get; private set; }
        public string CFSFileName { get; private set; }

        public byte[] Data { get; private set; }

        public string FilePath { get; private set; }
        public bool Overwrite { get; set; }
        public string Url { get; set; }
        public string[] ViewFields { get; set; }

        public bool UseCFS
        {
            get
            {
                return Data == null || Data.Length == 0;
            }
        }
    }

    internal class ItemCreateQuery
    {
        public static implicit operator ItemCreateQuery(SPListItemCreateOptions options)
        {
            return new ItemCreateQuery
            {
                FolderUrl = options.FolderUrl,
                Name = options.Name,
                IsFolder = options.IsFolder,
                Url = options.Url
            };
        }

        public string FolderUrl { get; set; }
        public string Name { get; set; }
        public bool? IsFolder { get; set; }
        public string Url { get; set; }
        public string[] ViewFields { get; set; }
    }

    internal class ItemGetQuery
    {
        public static implicit operator ItemGetQuery(SPListItemGetOptions options)
        {
            var query = options.ItemId.HasValue ? new ItemGetQuery(options.ItemId.Value) : new ItemGetQuery(options.UniqueId);
            query.Url = options.Url;
            return query;
        }

        public static implicit operator ItemGetQuery(DocumentGetOptions options)
        {
            var query = options.Id.HasValue ? new ItemGetQuery(options.Id.Value) : new ItemGetQuery(options.ContentId);
            query.Url = options.Url;
            query.ViewFields = Documents.ViewFields;
            return query;
        }

        public ItemGetQuery(int id)
        {
            Id = id;
        }

        public ItemGetQuery(Guid contentId)
        {
            ContentId = contentId;
        }

        public int? Id { get; private set; }
        public Guid ContentId { get; private set; }
        public string Url { get; set; }
        public string[] ViewFields { get; set; }
    }

    internal class ItemImportQuery : ItemGetQuery
    {
        public ItemImportQuery(int id) : base(id) { }
        public ItemImportQuery(Guid contentId) : base(contentId) { }

        /// <summary>
        /// The name of the field that should be used as a content key
        /// </summary>
        public string ContentKeyFieldName { get; set; }
    }

    internal class ItemUpdateQuery : ItemGetQuery
    {
        public static implicit operator ItemUpdateQuery(DocumentUpdateOptions options)
        {
            var query = options.Id.HasValue ? new ItemUpdateQuery(options.Id.Value, options.Fields) : new ItemUpdateQuery(options.ContentId, options.Fields);
            query.Url = options.Url;
            return query;
        }

        public static implicit operator ItemUpdateQuery(SPListItemUpdateOptions options)
        {
            var query = options.ItemId.HasValue ? new ItemUpdateQuery(options.ItemId.Value, options.Fields) : new ItemUpdateQuery(options.UniqueId, options.Fields);
            query.Url = options.Url;
            return query;
        }

        public ItemUpdateQuery(int id, IDictionary fields)
            : base(id)
        {
            Fields = fields;
        }

        public ItemUpdateQuery(Guid contentId, IDictionary fields)
            : base(contentId)
        {
            Fields = fields;
        }

        public IDictionary Fields { get; set; }
    }

    internal class ItemDeleteQuery
    {
        public static implicit operator ItemDeleteQuery(DocumentGetOptions options)
        {
            var query = options.Id.HasValue ? new ItemDeleteQuery(new[] { options.Id.Value }) : new ItemDeleteQuery(new[] { options.ContentId });
            query.Url = options.Url;
            return query;
        }

        public static implicit operator ItemDeleteQuery(SPListItemDeleteOptions options)
        {
            var query = new ItemDeleteQuery(options.ItemIds, options.ContentIds) { Url = options.Url };
            return query;
        }

        public ItemDeleteQuery(IEnumerable<Guid> contentIds)
        {
            ItemIds = new List<int>();
            ContentIds = new List<Guid>(contentIds);
        }

        public ItemDeleteQuery(IEnumerable<int> ids)
        {
            ItemIds = new List<int>(ids);
            ContentIds = new List<Guid>();
        }

        public ItemDeleteQuery(IEnumerable<int> ids, IEnumerable<Guid> contentIds)
        {
            ContentIds = new List<Guid>(contentIds);
            ItemIds = new List<int>(ids);
        }

        public List<int> ItemIds { get; private set; }
        public List<Guid> ContentIds { get; private set; }
        public string Url { get; set; }
    }

    internal class ItemListQuery
    {
        public static implicit operator ItemListQuery(SPListItemCollectionOptions options)
        {
            var query = new SPCamlQuery(options.PageSize)
            {
                Scope = SPCamlQuery.ViewScope.Default,
                SortBy = options.SortBy,
                SortOrder = options.SortOrder,
                ViewFields = ListItems.ViewFields
            };

            if (options.ViewFields != null && options.ViewFields.Count > 0)
            {
                query.ViewFields = options.ViewFields.ToArray();
            }

            if (!string.IsNullOrEmpty(options.ViewQuery))
            {
                query.Where = options.ViewQuery;
            }

            return new ItemListQuery
            {
                PageIndex = options.PageIndex,
                PageSize = options.PageSize,
                Url = options.Url,
                CamlQuery = query
            };
        }

        public static implicit operator ItemListQuery(DocumentListOptions options)
        {
            var query = new SPCamlQuery(options.PageSize)
            {
                Scope = SPCamlQuery.ViewScope.Default,
                SortBy = options.SortBy,
                SortOrder = options.SortOrder,
                GroupBy = "ContentType",
                GroupOrder = SortOrder.Descending,
                ViewFields = Documents.ViewFields,
                FolderPath = String.Concat("/", options.FolderPath.Trim('/')),
            };

            return new ItemListQuery
            {
                PageIndex = options.PageIndex,
                PageSize = options.PageSize,
                Url = options.Url,
                CamlQuery = query
            };
        }

        public string Url { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        public SPCamlQuery CamlQuery { get; set; }
    }

    internal interface IListItemService : IIndexable<SPListItem>
    {
        void Add(Guid listId, ItemImportQuery options);

        SPListItem Create(Guid listId, ItemCreateQuery options);
        SPListItem Create(Guid listId, DocumentCreateQuery options);

        SPListItem Get(Guid listId, ItemGetQuery options);

        SPPagedList<SPListItem> List(Guid listId, ItemListQuery options);

        SPListItem Update(Guid listId, ItemUpdateQuery options);

        void Delete(Guid listId, ItemDeleteQuery options);

        bool CanEdit(Guid listId, ItemGetQuery options);

        List<ItemBase> GetItemsFromSharePoint(ListBase list);
    }

    internal class SPItemService : IListItemService
    {
        private readonly ICredentialsManager credentials;
        private readonly IListDataService listDataService;
        private readonly IListItemDataService listItemDataService;

        public SPItemService()
            : this(ServiceLocator.Get<ICredentialsManager>(), ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<IListItemDataService>())
        {
        }

        public SPItemService(ICredentialsManager credentials, IListDataService listDataService, IListItemDataService listItemDataService)
        {
            this.credentials = credentials;
            this.listDataService = listDataService;
            this.listItemDataService = listItemDataService;
        }

        public static Expression<Func<ListItem, object>>[] InstanceQuery
        {
            get
            {
                return new Expression<Func<ListItem, object>>[]{
                    listItem => listItem,
                    listItem => listItem.ContentType,
                    listItem => listItem.DisplayName,
                    listItem => listItem.FieldValuesAsHtml,
                    listItem => listItem.FieldValuesAsText,
                    listItem => listItem.FieldValuesForEdit,
                    listItem => listItem.File,
                    listItem => listItem.Id,
                    listItem => listItem.ParentList.Id
                };
            }
        }

        #region IListItemService

        public void Add(Guid listId, ItemImportQuery options)
        {
            var listItem = Get(listId, options);
            if (listItem == null) return;

            var itemBase = new ItemBase(listId, listItem.UniqueId, listItem.Id, listItem.Modified);
            var key = listItem.DisplayName;

            if (!string.IsNullOrEmpty(options.ContentKeyFieldName) && listItem.HasValue(options.ContentKeyFieldName))
            {
                var fieldValue = listItem.ValueAsText(options.ContentKeyFieldName);
                if (!string.IsNullOrEmpty(fieldValue) && !fieldValue.Equals("(no title)", StringComparison.CurrentCultureIgnoreCase))
                {
                    key = fieldValue;
                }
            }

            if (!key.Equals("(no title)", StringComparison.CurrentCultureIgnoreCase))
            {
                itemBase.ContentKey = key;
            }

            listItemDataService.AddUpdate(itemBase);
        }

        public SPListItem Create(Guid listId, ItemCreateQuery options)
        {
            var url = EnsureUrl(options.Url, listId);

            SPListItem item;
            
            try
            {
                ItemBase itemBase;

                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spweb = clientContext.Web;
                    var spsite = clientContext.Site;

                    clientContext.Load(spweb, w => w.Id);
                    clientContext.Load(spsite, s => s.Id);

                    var splist = clientContext.Web.Lists.GetById(listId);
                    var creationOptions = new ListItemCreationInformation();

                    if (!string.IsNullOrEmpty(options.FolderUrl))
                    {
                        creationOptions.FolderUrl = options.FolderUrl;
                    }
                    if (!string.IsNullOrEmpty(options.Name))
                    {
                        creationOptions.LeafName = options.Name;
                    }
                    if (options.IsFolder.HasValue)
                    {
                        creationOptions.UnderlyingObjectType = options.IsFolder.Value ? FileSystemObjectType.Folder : FileSystemObjectType.File;
                    }

                    var splistItem = splist.AddItem(creationOptions);
                    
                    clientContext.Load(splistItem, spitem => spitem.Id);
                    splistItem.Update();

                    clientContext.ExecuteQuery();

                    item = GetListItem(listId, splistItem.Id, options.ViewFields, clientContext);

                    itemBase = new ItemBase(listId, item.UniqueId, item.Id, DateTime.UtcNow);

                    if (!string.IsNullOrEmpty(item.DisplayName))
                    {
                        itemBase.ContentKey = item.DisplayName;
                    }
                }

                listItemDataService.AddUpdate(itemBase);
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.Create() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }

            return item;
        }

        public SPListItem Create(Guid listId, DocumentCreateQuery options)
        {
            var url = EnsureUrl(options.Url, listId);

            SPListItem item;

            try
            {
                ItemBase itemBase;

                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var filePath = options.FilePath;

                    if (options.UseCFS)
                    {
                        UploadFile(clientContext, filePath, options.CFSFileStore, options.CFSFilePath, options.CFSFileName, options.Overwrite);
                    }
                    else
                    {
                        UploadFile(clientContext, filePath, options.Data, options.Overwrite);
                    }

                    var spweb = clientContext.Web;
                    var file = spweb.GetFileByServerRelativeUrl(filePath);
                    var createdItem = file.ListItemAllFields;

                    clientContext.Load(spweb, w => w.Id);
                    clientContext.Load(createdItem, spitem => spitem.Id);

                    clientContext.ExecuteQuery();

                    item = GetListItem(listId, createdItem.Id, options.ViewFields, clientContext);

                    itemBase = new ItemBase(listId, item.UniqueId, item.Id, DateTime.UtcNow);

                    if (!string.IsNullOrEmpty(item.DisplayName))
                    {
                        itemBase.ContentKey = item.DisplayName;
                    }
                }

                listItemDataService.AddUpdate(itemBase);
            }
            catch (Exception ex)
            {
                if (ex is ClientRequestException && ex.Message == "The file already exists.")
                {
                    throw new SPFileAlreadyExistsException(ex.Message, ex) { FilePath = options.FilePath };
                }

                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.Create() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }

            return item;
        }

        public SPListItem Get(Guid listId, ItemGetQuery options)
        {
            string url = EnsureUrl(options.Url, listId);

            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    string[] viewFields;
                    if (options.ViewFields != null && options.ViewFields.Any())
                    {
                        viewFields = options.ViewFields;
                    }
                    else
                    {
                        var splist = clientContext.Web.Lists.GetById(listId);
                        var fieldsQuery = clientContext.LoadQuery(splist.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"));
                        clientContext.ExecuteQuery();
                        viewFields = fieldsQuery.Select(field => field.InternalName).ToArray();
                    }
                    return GetListItem(listId, options.Id, options.ContentId, viewFields, clientContext);
                }
            }
            catch (ServerUnauthorizedAccessException)
            {
                return null;
            }
            catch (Exception ex)
            {
                if (ex.IsUnauthorizedAccessException()) return null;

                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.Get({1}). The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        public SPPagedList<SPListItem> List(Guid listId, ItemListQuery options)
        {
            string url = EnsureUrl(options.Url, listId);

            var items = new SPPagedList<SPListItem>();

            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var splist = clientContext.Web.Lists.GetById(listId);

                    var folder = options.CamlQuery != null && options.CamlQuery.FolderPath != null && !String.IsNullOrEmpty(options.CamlQuery.FolderPath.Trim('/'))
                        ? clientContext.Web.GetFolderByServerRelativeUrl(options.CamlQuery.FolderPath)
                        : splist.RootFolder;

                    clientContext.Load(folder, f => f.ItemCount);

                    var pageInfo = GetPageInfo(options.PageSize, options.PageIndex, options.CamlQuery, splist);
                    var listItems = splist.GetItems(options.CamlQuery.ToSPCamlRequest(pageInfo));

                    clientContext.Load(listItems);
                    clientContext.Load(listItems,
                        spitems => spitems.ListItemCollectionPosition,
                        spitems =>
                        spitems.Include(item => item.ParentList.Id, item => item.File.CheckedOutByUser));

                    var fieldsQuery = clientContext.LoadQuery(splist.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"));

                    // 1. Retrieving List Items
                    clientContext.ExecuteQuery();

                    var fieldList = fieldsQuery.ToList();
                    var userLookupIds = new List<int>();

                    foreach (var listItem in listItems)
                    {
                        var spListItem = new SPListItem(listItem, fieldList);
                        var author = spListItem.Value("Author") as FieldUserValue;
                        if (author != null && !userLookupIds.Contains(author.LookupId))
                        {
                            userLookupIds.Add(author.LookupId);
                        }

                        var editor = spListItem.Value("Editor") as FieldUserValue;
                        if (editor != null && !userLookupIds.Contains(editor.LookupId))
                        {
                            userLookupIds.Add(editor.LookupId);
                        }

                        items.Add(spListItem);
                    }

                    items.TotalCount = folder.ItemCount;
                    items.PageSize = options.PageSize;
                    items.PageIndex = options.PageIndex;
                    items.PageInfo = listItems.ListItemCollectionPosition != null
                        ? listItems.ListItemCollectionPosition.PagingInfo
                        : string.Empty;

                    // 2. Retrieving Authors and Editors
                    if (userLookupIds.Any())
                    {
                        PopulateAuthorsEditors(clientContext, items, userLookupIds);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                return items;
            }
            catch (Exception ex)
            {
                if (ex.IsUnauthorizedAccessException()) return null;

                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.List() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }

            return items;
        }

        public SPListItem Update(Guid listId, ItemUpdateQuery options)
        {
            string url = EnsureUrl(options.Url, listId);

            SPListItem item;

            try
            {
                using (var spcontext = new SPContext(url, credentials.Get(url)))
                {
                    var spweb = spcontext.Web;
                    spcontext.Load(spweb, w => w.Id);

                    var spsite = spcontext.Site;
                    spcontext.Load(spsite, s => s.Id);

                    var splist = spcontext.Web.Lists.GetById(listId);
                    ListItem splistItem;
                    if (options.Id.HasValue)
                    {
                        splistItem = splist.GetItemById(options.Id.Value);
                    }
                    else
                    {
                        var splistItemCollection = splist.GetItems(CAMLQueryBuilder.GetItem(options.ContentId, options.ViewFields));
                        spcontext.Load(splistItemCollection, spitems => spitems.Include(spitem => spitem.Id));
                        spcontext.ExecuteQuery();

                        splistItem = splistItemCollection.FirstOrDefault();
                    }

                    if (splistItem != null)
                    {
                        foreach (var fieldName in options.Fields.Keys)
                        {
                            splistItem[fieldName.ToString()] = options.Fields[fieldName];
                        }
                        splistItem.Update();
                    }

                    item = GetListItem(listId, options.Id, options.ContentId, options.ViewFields, spcontext);
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.Update() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }

            return item;
        }

        public void Delete(Guid listId, ItemDeleteQuery options)
        {
            string url = EnsureUrl(options.Url, listId);

            try
            {
                using (var spcontext = new SPContext(url, credentials.Get(url)))
                {
                    var splist = spcontext.Web.Lists.GetById(listId);

                    var lazyLoadedItemsToRemove = new List<IEnumerable<ListItem>>();
                    foreach (var splistItem in options.ItemIds.Select(splist.GetItemById))
                    {
                        spcontext.Load(splistItem, item => item["UniqueId"]);
                        lazyLoadedItemsToRemove.Add(new[] { splistItem });
                    }

                    lazyLoadedItemsToRemove.AddRange(options.ContentIds
                        .Select(uniqueId => splist.GetItems(CAMLQueryBuilder.GetItem(uniqueId, new[] { "UniqueId" })))
                            .Select(splistItemCollection =>
                                spcontext.LoadQuery(splistItemCollection.Include(item => item["UniqueId"]))));

                    spcontext.ExecuteQuery();

                    foreach (var listItem in lazyLoadedItemsToRemove.Select(lazyItem => lazyItem.FirstOrDefault()))
                    {
                        Guid uniqueId;
                        if (listItem != null && Guid.TryParse(listItem["UniqueId"].ToString(), out uniqueId))
                        {
                            listItem.DeleteObject();
                            listItemDataService.Delete(uniqueId);
                        }
                    }
                    spcontext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.Delete() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        public bool CanEdit(Guid listId, ItemGetQuery options)
        {
            string url = EnsureUrl(options.Url, listId);
            try
            {
                using (var spcontext = new SPContext(url, credentials.Get(url)))
                {
                    BasePermissions permissions = null;
                    var splist = spcontext.Web.Lists.GetById(listId);
                    if (options.Id.HasValue)
                    {
                        var splistItem = splist.GetItemById(options.Id.Value);
                        spcontext.Load(splistItem, item => item.EffectiveBasePermissions);
                        spcontext.ExecuteQuery();
                        permissions = splistItem.EffectiveBasePermissions;
                    }
                    else
                    {
                        var splistItemCollection = splist.GetItems(CAMLQueryBuilder.GetItem(options.ContentId, options.ViewFields));
                        var splistItems = spcontext.LoadQuery(splistItemCollection.Include(spitem => spitem.Id, item => item.EffectiveBasePermissions));
                        spcontext.ExecuteQuery();
                        var splistItem = splistItems.FirstOrDefault();
                        if (splistItem != null)
                        {
                            permissions = splistItem.EffectiveBasePermissions;
                        }
                    }
                    return permissions != null && permissions.Has(PermissionKind.EditListItems);
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.CanEdit() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        #endregion

        #region IIndexable

        public PagedList<SPListItem> ListItemsToReindex(Guid typeId, int batchSize, string[] viewFields = null)
        {
            var lists = listDataService.List(typeId);

            SyncSharePointItems(lists);

            var listItemsPagedList = new PagedList<SPListItem>();
            var itemsToReindex = listItemDataService.ListItemsToReindex(batchSize, lists.Select(_ => _.Id).ToArray());
            try
            {
                if (itemsToReindex != null && itemsToReindex.Count > 0)
                {
                    foreach (var itemBase in itemsToReindex)
                    {
                        try
                        {
                            var listItem = Get(itemBase.ApplicationId, new ItemGetQuery(itemBase.UniqueId)
                                                {
                                                    ViewFields = viewFields
                                                });
                            listItemsPagedList.Add(listItem);
                        }
                        catch { }
                    }

                    listItemsPagedList.PageIndex = itemsToReindex.PageIndex;
                    listItemsPagedList.PageSize = itemsToReindex.PageSize;
                    listItemsPagedList.TotalCount = itemsToReindex.TotalCount;
                }
            }
            catch (SPDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.ListItemsToReindex() method for ApplicationTypeId: {1}. The exception message is: {2}", ex.GetType(), typeId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }

            return listItemsPagedList;
        }

        public void UpdateIndexingStatus(Guid[] contentIds, bool isIndexed)
        {
            listItemDataService.UpdateIndexingStatus(contentIds, isIndexed);
        }

        #endregion

        #region External Source

        public List<ItemBase> GetItemsFromSharePoint(ListBase list)
        {
            const int batchSize = 1000;
            var itemsFromSharePoint = new List<ItemBase>();
            try
            {
                using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
                {
                    var splist = clientContext.Web.Lists.GetById(list.Id);

                    int pageIndex = 0;
                    var query = new SPCamlQuery(batchSize)
                    {
                        Scope = SPCamlQuery.ViewScope.RecursiveAll,
                        ViewFields = new[] { "Title", "UniqueId", "Id", "ContentTypeId" }
                    };

                    bool hasItems;
                    do
                    {
                        string pageInfo = GetPageInfo(batchSize, pageIndex, query, splist);
                        var listItems = splist.GetItems(query.ToSPCamlRequest(pageInfo));

                        clientContext.Load(listItems,
                            items => items.ListItemCollectionPosition,
                            items => items.Include(
                                item => item.Id,
                                item => item.DisplayName,
                                item => item["Title"],
                                item => item["UniqueId"],
                                item => item["ContentTypeId"],
                                item => item["Modified"]));
                        clientContext.ExecuteQuery();

                        foreach (var item in listItems)
                        {
                            var contentType = item["ContentTypeId"];
                            var isFolder = contentType != null && contentType.ToString().StartsWith(Document.FolderContentType);

                            Guid id;
                            if (item["UniqueId"] != null && Guid.TryParse(item["UniqueId"].ToString(), out id))
                            {
                                var itemBase = new ItemBase(list.Id, id, item.Id, Convert.ToDateTime(item["Modified"]))
                                                {
                                                    ContentKey = GetListItemTitle(item),
                                                    IsIndexable = !isFolder
                                                };
                                itemsFromSharePoint.Add(itemBase);
                            }
                        }

                        pageIndex++;
                        hasItems = listItems.ListItemCollectionPosition != null && !string.IsNullOrEmpty(listItems.ListItemCollectionPosition.PagingInfo);
                    }
                    while (hasItems);
                }
            }
            catch (Exception ex)
            {
                if (ex.IsUnauthorizedAccessException()) return itemsFromSharePoint;

                if (ex.Message.StartsWith("List does not exist."))
                {
                    if (list.TypeId == LibraryApplicationType.Id)
                    {
                        var libraryApp = new Library { Id = list.Id, GroupId = list.GroupId, Url = list.SPWebUrl };
                        PublicApi.Libraries.Events.OnAfterDelete(libraryApp);
                    }

                    if (list.TypeId == ListApplicationType.Id)
                    {
                        var listApp = new SPList { Id = list.Id, GroupId = list.GroupId, SPWebUrl = list.SPWebUrl };
                        PublicApi.Lists.Events.OnAfterDelete(listApp);
                    }

                    ServiceLocator.Get<IListService>().Delete(list.Id, false);
                }
                else
                {
                    var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.GetItemsFromSharePoint() method for ListId: {1} UserId: {2}. The exception message is: {3}",
                        ex.GetType(),
                        list.Id,
                        SPCoreService.UserId,
                        ex.Message);

                    SPLog.RoleOperationUnavailable(ex, message);

                    throw new SPInternalException(message, ex);
                }
            }

            return itemsFromSharePoint;
        }

        private string GetListItemTitle(ListItem item)
        {
            if (string.IsNullOrWhiteSpace(item.DisplayName)) return item.DisplayName;

            if (item.FieldValues.ContainsKey("Title") && item.FieldValues["Title"] != null)
            {
                var title = item.FieldValues["Title"].ToString();
                if (!string.IsNullOrWhiteSpace(title) && !title.Equals("(no title)", StringComparison.InvariantCultureIgnoreCase))
                {
                    return title;
                }
            }
            return null;
        }

        #endregion

        private void UploadFile(SPContext spcontext, string filePath, byte[] data, bool overwrite = true)
        {
            using (var stream = new MemoryStream(data, 0, data.Length))
            {
                UploadFile(spcontext, stream, filePath, overwrite);
            }
        }

        private void UploadFile(SPContext spcontext, string filePath, string cfsFileStore, string cfsFilePath, string cfsFileName, bool overwrite)
        {
            var evoFile = TEApi.Cfs.Get(cfsFileStore, cfsFilePath, cfsFileName);
            using (var stream = evoFile.OpenReadStream())
            {
                UploadFile(spcontext, stream, filePath, overwrite);
            }
        }

        private void UploadFile(SPContext spcontext, Stream stream, string filePath, bool overwrite)
        {
            var creds = credentials.Get(spcontext.Url);
            if (creds is Components.AuthenticationUtil.Methods.OAuth)
            {
                var folder = filePath.Substring(0, filePath.LastIndexOf('/'));
                var fileName = filePath.Substring(filePath.LastIndexOf('/') + 1);
                OAuthSaveBinaryDirect(spcontext, folder, fileName, stream, overwrite);
            }
            else if (creds is Components.AuthenticationUtil.Methods.SAML)
            {
                try
                {
                    //SharePoint 2010
                    Microsoft.SharePoint.Client.File.SaveBinaryDirect(spcontext, string.Format("/{0}", filePath.TrimStart('/')), stream, overwrite);
                }
                catch (Exception)
                {
                    //SharePoint 2013
                    var folder = filePath.Substring(0, filePath.LastIndexOf('/'));
                    var fileName = filePath.Substring(filePath.LastIndexOf('/') + 1);
                    SAMLSaveBinaryDirect(spcontext, folder, fileName, stream, overwrite);
                }
            }
            else
            {
                Microsoft.SharePoint.Client.File.SaveBinaryDirect(spcontext, string.Format("/{0}", filePath.TrimStart('/')), stream, overwrite);
            }
        }

        private static void OAuthSaveBinaryDirect(SPContext context, string serverRelativeFolder, string fileName, Stream stream, bool overwrite)
        {
            var header = new Dictionary<string, string> { { "Authorization", "Bearer " + context.OAuthToken } };
            SaveBinaryDirect(context.Url, serverRelativeFolder, fileName, stream, overwrite, header);
        }

        private static void SAMLSaveBinaryDirect(SPContext context, string serverRelativeFolder, string fileName, Stream stream, bool overwrite)
        {
            var header = new Dictionary<string, string> { { "Cookie", string.Format("FedAuth={0}", context.SAMLToken) } };
            SaveBinaryDirect(context.Url, serverRelativeFolder, fileName, stream, overwrite, header);
        }

        private static void SaveBinaryDirect(string url, string serverRelativeFolder, string fileName, Stream stream, bool overwrite, Dictionary<string, string> header)
        {
            var requestDigest = GetFormDigest(url, header);

            if (string.IsNullOrEmpty(requestDigest)) throw new Exception("Could not get Form Digest.");

            var requestUrl = string.Format("{0}/_api/web/GetFolderByServerRelativeUrl('{1}')/Files/Add(url='{2}', overwrite={3})", url, serverRelativeFolder, fileName, overwrite.ToString().ToLowerInvariant());
            var request = WebRequest.Create(requestUrl) as HttpWebRequest;
            request.ReadWriteTimeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
            request.Method = "POST";
            request.Accept = "*/*";
            request.ContentType = "application/json;odata=verbose";
            request.ContentLength = stream.Length;

            request.Headers.Add("X-RequestDigest", requestDigest);

            foreach (var head in header)
            {
                request.Headers.Add(head.Key, head.Value);
            }

            using (var requestStream = request.GetRequestStream())
            {
                const int batchSize = 100 * 1024;
                var buffer = new byte[batchSize];
                int count;
                while ((count = stream.Read(buffer, 0, batchSize)) > 0)
                {
                    requestStream.Write(buffer, 0, count);
                }
            }

            using (var response = request.GetResponse() as HttpWebResponse)
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        reader.ReadToEnd();
                    }
                }
            }
        }

        private void SyncSharePointItems(IEnumerable<ListBase> lists)
        {
            foreach (var list in lists)
            {
                var itemsFromSharePoint = GetItemsFromSharePoint(list);
                var itemsFromEvolution = listItemDataService.List(list.Id);

                var compareStatus = itemsFromEvolution.Compare(itemsFromSharePoint);
                if (compareStatus == CompareStatus.IsEqual)
                {
                    continue;
                }

                if (compareStatus.HasFlag(CompareStatus.HasNew))
                {
                    var itemsToAdd = itemsFromEvolution.GetItemsToAdd(itemsFromSharePoint);
                    listItemDataService.AddUpdate(itemsToAdd);
                    Reset(list, itemsToAdd);
                }

                if (compareStatus.HasFlag(CompareStatus.HasDeleted))
                {
                    foreach (var evolutionItem in itemsFromEvolution.GetItemsToDelete(itemsFromSharePoint))
                    {
                        if (list.TypeId == LibraryApplicationType.Id)
                        {
                            var document = PublicApi.Documents.Get(list.Id, new DocumentGetOptions(evolutionItem.UniqueId));
                            PublicApi.Documents.Events.OnAfterDelete(document);
                        }

                        if (list.TypeId == ListApplicationType.Id)
                        {
                            var listItem = PublicApi.ListItems.Get(list.Id, new SPListItemGetOptions(evolutionItem.UniqueId));
                            PublicApi.ListItems.Events.OnAfterDelete(listItem);
                        }

                        listItemDataService.Delete(evolutionItem.UniqueId);
                    }

                    if (list.TypeId == LibraryApplicationType.Id)
                    {
                        PublicApi.Documents.ExpireTags(list.Id);
                    }

                    if (list.TypeId == ListApplicationType.Id)
                    {
                        PublicApi.ListItems.ExpireTags(list.Id);
                    }
                }

                if (compareStatus == CompareStatus.HasUpdates)
                {
                    var itemsToUpdate = itemsFromEvolution.GetItemsToUpdate(itemsFromSharePoint);
                    Reset(list, itemsToUpdate);
                }
            }
        }

        private static void Reset(ListBase list, List<ItemBase> items)
        {
            if (list.TypeId == LibraryApplicationType.Id)
            {
                PublicApi.Documents.ExpireTags(list.Id);
                foreach (var item in items)
                {
                    var document = PublicApi.Documents.Get(list.Id, new DocumentGetOptions(item.UniqueId));
                    PublicApi.Documents.Events.OnAfterUpdate(document);
                }
            }

            if (list.TypeId == ListApplicationType.Id)
            {
                PublicApi.ListItems.ExpireTags(list.Id);
                foreach (var item in items)
                {
                    var listItem = PublicApi.ListItems.Get(list.Id, new SPListItemGetOptions(item.UniqueId));
                    PublicApi.ListItems.Events.OnAfterUpdate(listItem);
                }
            }
        }

        #region Get Item related methods

        private static SPListItem GetListItem(Guid listId, int itemId, string[] viewFields, SPContext spcontext)
        {
            return GetListItem(listId, itemId, Guid.Empty, viewFields, spcontext);
        }

        private static SPListItem GetListItem(Guid listId, int? itemId, Guid uniqueId, string[] viewFields, SPContext spcontext)
        {
            SPListItem item;

            var splist = spcontext.Web.Lists.GetById(listId);
            var splistItemCollection = splist.GetItems(itemId.HasValue ?
                CAMLQueryBuilder.GetItem(itemId.Value, viewFields) :
                CAMLQueryBuilder.GetItem(uniqueId, viewFields));

            var splistItems = spcontext.LoadQuery(splistItemCollection.IncludeWithDefaultProperties(InstanceQuery));
            var fieldsQuery = spcontext.LoadQuery(splist.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"));

            spcontext.ExecuteQuery();

            var items = splistItems.ToArray();

            if (items.Any())
            {
                item = new SPListItem(items.First(), fieldsQuery.ToList());

                // Populate Authors and Editors
                PopulateAuthorsEditors(spcontext, item);
            }
            else
            {
                string errorMsg = itemId.HasValue ?
                    String.Format("The InternalApi.SPItemService.Get() operation failed for ListId = {0}, ItemId = {1} the List or Item may no longer exist.", listId, itemId) :
                    String.Format("The InternalApi.SPItemService.Get() operation failed for ListId = {0}, UniqueId = {1} the List or Item may no longer exist.", listId, uniqueId);
                throw new SPInternalException(errorMsg);
            }

            return item;
        }

        private static void PopulateAuthorsEditors(SPContext clientContext, IEnumerable<SPListItem> listItems, IEnumerable<int> userLookupIds)
        {
            try
            {
                var users = LoadUserProfiles(clientContext, userLookupIds);
                foreach (var listItem in listItems)
                {
                    if (listItem.Author != null && users.ContainsKey(listItem.Author.LookupId))
                    {
                        listItem.Author.Initialize(users[listItem.Author.LookupId]);
                    }
                    if (listItem.Editor != null && users.ContainsKey(listItem.Editor.LookupId))
                    {
                        listItem.Editor.Initialize(users[listItem.Editor.LookupId]);
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.PopulateAuthorsEditors() method. The exception message is: {1}", ex.GetType(), ex.Message);
                SPLog.UnKnownError(ex, message);
            }
        }

        private static void PopulateAuthorsEditors(SPContext clientContext, SPListItem listItem)
        {
            var lookupIds = new List<int>();
            var author = listItem.Value("Author") as FieldUserValue;
            if (author != null)
            {
                lookupIds.Add(author.LookupId);
            }

            var editor = listItem.Value("Editor") as FieldUserValue;
            if (editor != null)
            {
                lookupIds.Add(editor.LookupId);
            }

            if (lookupIds.Count > 0)
            {
                try
                {
                    var userLookupIds = LoadUserProfiles(clientContext, lookupIds);
                    if (author != null && userLookupIds.ContainsKey(author.LookupId))
                    {
                        listItem.Author.Initialize(userLookupIds[author.LookupId]);
                    }
                    if (editor != null && userLookupIds.ContainsKey(listItem.Editor.LookupId))
                    {
                        listItem.Editor.Initialize(userLookupIds[editor.LookupId]);
                    }
                }
                catch (Exception ex)
                {
                    var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemService.PopulateAuthorsEditors() method. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }
        }

        private static Dictionary<int, ListItem> LoadUserProfiles(SPContext clientContext, IEnumerable<int> userLookupIds)
        {
            clientContext.ExecuteQuery();

            var userItemCollection = clientContext.Web.SiteUserInfoList.GetItems(CAMLQueryBuilder.ListItems(userLookupIds));
            var userItems = clientContext.LoadQuery(userItemCollection.Include(Author.InstanceQuery));

            clientContext.ExecuteQuery();

            return userItems.ToDictionary(_ => _.Id);
        }

        #endregion

        private static string GetPageInfo(int pageSize, int pageIndex, SPCamlQuery spcamlQuery, List splist)
        {
            if (pageIndex <= 0) return string.Empty;

            var tempQuery = new SPCamlQuery(spcamlQuery)
            {
                ViewFields = null,
                RowLimit = pageSize * pageIndex
            };

            var tempItems = splist.GetItems(tempQuery.ToSPCamlRequest());
            splist.Context.Load(tempItems, items => items.ListItemCollectionPosition);
            splist.Context.ExecuteQuery();

            return tempItems.ListItemCollectionPosition != null ? tempItems.ListItemCollectionPosition.PagingInfo : string.Empty;
        }

        private static string GetFormDigest(string url, Dictionary<string, string> header)
        {
            string formDigestValue = null;

            var formDigestPattern = new Regex(@"""FormDigestValue""\:""([^""]+)""", RegexOptions.Compiled | RegexOptions.Singleline);

            var requestUrl = string.Format("{0}/_api/contextinfo", url);
            var request = WebRequest.Create(requestUrl) as HttpWebRequest;

            if (request == null) return null;

            request.ReadWriteTimeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
            request.Method = "POST";
            request.Accept = "application/json;odata=verbose";
            request.ContentType = "application/json;odata=verbose";
            request.ContentLength = 0;

            foreach (var head in header)
            {
                request.Headers.Add(head.Key, head.Value);
            }

            try
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response != null)
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream != null)
                            {
                                using (var reader = new StreamReader(responseStream))
                                {
                                    var result = reader.ReadToEnd();
                                    var match = formDigestPattern.Match(result);
                                    formDigestValue = match.Success ? match.Groups[1].Value.Trim() : string.Empty;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return formDigestValue;
        }

        private string EnsureUrl(string url, Guid listId)
        {
            var notEmptyUrl = !String.IsNullOrEmpty(url) ? url : GetUrlByListId(listId);

            if (string.IsNullOrEmpty(notEmptyUrl))
                throw new InvalidOperationException("Url cannot be empty.");

            return notEmptyUrl;
        }

        private string GetUrlByListId(Guid listId)
        {
            var list = listDataService.Get(listId);
            if (list != null)
            {
                list.Validate();
                return list.SPWebUrl;
            }
            return null;
        }
    }
}
