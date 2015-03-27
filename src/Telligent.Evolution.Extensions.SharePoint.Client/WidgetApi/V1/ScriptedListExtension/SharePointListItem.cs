using Microsoft.SharePoint.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointListItemExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_listItem"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointListItem>(); }
        }

        public string Name
        {
            get { return "SharePoint List Item Extension (sharepoint_v1_listItem)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to use the SharePoint Client Object Model."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointListItem : ICacheable
    {
        SPListItem Get(SPList list, string id, IDictionary options);

        ApiList<SPListItem> List(SPList list, IDictionary options);

        SPListItem Create(SPList list);

        SP.Folder NewFolder(SPList list, string folderName, string currentDir);

        /// <summary>
        /// Checks that folder with selected name does not exist in the directory
        /// </summary>
        /// <param name="list"></param>
        /// <param name="folderName">Folder name to valid</param>
        /// <param name="currentDir"></param>
        /// <returns></returns>
        bool IsFolderValid(SPList list, string folderName, string currentDir);

        SPListItem Update(SPList list, SPListItem item);

        void Delete(SPList list, SPListItem item);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointListItem : ISharePointListItem
    {
        private const string SharePointListItemCacheId = "SharePointListItem_GetList";
        private readonly ICredentialsManager credentials;
        private readonly InternalApi.ICacheService cacheService;

        internal SharePointListItem() : this(ServiceLocator.Get<ICredentialsManager>(), ServiceLocator.Get<InternalApi.ICacheService>()) { }
        internal SharePointListItem(ICredentialsManager credentials, InternalApi.ICacheService cacheService)
        {
            this.credentials = credentials;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        [Obsolete("Use sharepoint_v2_listItem", true)]
        public SPListItem Get(SPList list, string listItemId,
            [Documentation(Name = "ViewFields", Type = typeof(List<string>))]
            IDictionary options)
        {
            int id;

            if (!int.TryParse(listItemId, out id))
                return null;

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var splist = clientContext.ToList(list.Id);
                IEnumerable<ListItem> itemQuery;
                if (options != null && options["ViewFields"] != null)
                {
                    var viewFields = (List<string>)options["ViewFields"];
                    itemQuery = clientContext.LoadQuery(splist.GetItems(CamlQuery.CreateAllItemsQuery())
                        .Where(item => item.Id == id)
                        .Include(CreateListItemLoadExpressions(viewFields)));
                }
                else
                {
                    itemQuery = clientContext.LoadQuery(splist.GetItems(CamlQuery.CreateAllItemsQuery())
                        .Where(item => item.Id == id)
                        .IncludeWithDefaultProperties(SPItemService.InstanceQuery));
                }
                var fieldsQuery = clientContext.LoadQuery(splist.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"));
                clientContext.ExecuteQuery();
                var listItem = itemQuery.FirstOrDefault();
                if (listItem != null)
                {
                    return new SPListItem(listItem, fieldsQuery.ToList());
                }
                return null;
            }
        }

        [Obsolete("Use sharepoint_v2_listItem", true)]
        public ApiList<SPListItem> List(SPList list,
            [Documentation(Name = "ViewFields", Type = typeof(List<string>)),
            Documentation(Name = "ViewQuery", Type = typeof(string))]
            IDictionary options)
        {

            if (list == null)
                return new ApiList<SPListItem>();

            var cacheId = string.Concat(SharePointListItemCacheId, string.Format("{0}_{1}", list.SPWebUrl, list.Id));
            var cacheList = (ApiList<SPListItem>)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (cacheList == null)
            {
                using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
                {
                    var sharepointList = clientContext.ToList(list.Id);
                    ListItemCollection listItemCollection;
                    if (options != null && options["ViewQuery"] != null)
                    {
                        var camlQuery = new CamlQuery
                            {
                                ViewXml = String.Format("<View><Query>{0}</Query></View>", options["ViewQuery"])
                            };
                        listItemCollection = sharepointList.GetItems(camlQuery);
                    }
                    else
                    {
                        listItemCollection = sharepointList.GetItems(CamlQuery.CreateAllItemsQuery());
                    }
                    IEnumerable<ListItem> items;
                    if (options != null && options["ViewFields"] != null)
                    {
                        var viewFields = (List<string>)options["ViewFields"];
                        items = clientContext.LoadQuery(listItemCollection.Include(CreateListItemLoadExpressions(viewFields)));
                    }
                    else
                    {
                        items = clientContext.LoadQuery(listItemCollection.Include(SPItemService.InstanceQuery));
                    }
                    var userItemCollection = clientContext.Web.SiteUserInfoList.GetItems(CamlQuery.CreateAllItemsQuery());
                    IEnumerable<SP.ListItem> userItems = clientContext.LoadQuery(
                            userItemCollection.Include(new Expression<Func<ListItem, object>>[] { item => item.Id, item => item["Picture"] }));

                    var fieldsQuery = clientContext.LoadQuery(sharepointList.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"));
                    clientContext.ExecuteQuery();
                    var apiList = new ApiList<SPListItem>();
                    var fields = fieldsQuery.ToList();

                    foreach (var item in items)
                    {
                        apiList.Add(new SPListItem(item, fields));
                    }

                    cacheService.Put(cacheId, apiList, CacheScope.Context | CacheScope.Process, new[] { GetTag(list.Id) }, CacheTimeOut);
                    return apiList;
                }
            }

            return cacheList;
        }

        [Obsolete("Use sharepoint_v2_listItem", true)]
        public SPListItem Create(SPList list)
        {
            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var splist = clientContext.ToList(list.Id);
                var splistItem = splist.AddItem(null);
                splistItem.Update();
                clientContext.Load(splistItem, SPItemService.InstanceQuery);
                var fieldsQuery = clientContext.LoadQuery(splist.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"));
                clientContext.ExecuteQuery();

                cacheService.RemoveByTags(new[] { GetTag(list.Id) }, CacheScope.Context | CacheScope.Process);
                return new SPListItem(splistItem, fieldsQuery.ToList());
            }
        }

        [Obsolete("Use sharepoint_v2_listItem", true)]
        public SP.Folder NewFolder(SPList list, string folderName, string currentDir)
        {
            if (!IsFolderValid(list, folderName, currentDir))
                return null;

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                SP.List splist = clientContext.ToList(list.Id);
                SP.Folder parentFolder = clientContext.Web.GetFolderByServerRelativeUrl(currentDir);
                clientContext.Load(parentFolder);

                // add a new folder
                SP.Folder newFolder = parentFolder.Folders.Add(folderName);
                parentFolder.Update();
                clientContext.Load(newFolder);
                try
                {
                    clientContext.ExecuteQuery();
                }
                catch (Exception ex)
                {
                    EventLogs.Warn(String.Format("An exception of type {0} occurred while creating a new folder with a name '{1}' for a directory '{2}'. The exception message is: {3}", ex.GetType().Name, folderName, currentDir, ex.Message), "SharePointClient", 778, CSContext.Current.SettingsID);
                    return null;
                }

                cacheService.RemoveByTags(new[] { GetTag(list.Id) }, CacheScope.Context | CacheScope.Process);
                return newFolder;
            }
        }

        [Obsolete("Use sharepoint_v2_listItem", true)]
        public bool IsFolderValid(SPList list, string folderName, string currentDir)
        {
            folderName = folderName.Trim();
            if (String.IsNullOrEmpty(folderName))
            {
                return false;
            }
            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                SP.List splist = clientContext.ToList(list.Id);
                SP.Folder parentFolder = clientContext.Web.GetFolderByServerRelativeUrl(currentDir);
                clientContext.Load(parentFolder);
                var subfolders = clientContext.LoadQuery(parentFolder.Folders.Where(folder => folder.Name == folderName));
                try
                {
                    clientContext.ExecuteQuery();
                }
                catch (Exception ex)
                {
                    EventLogs.Warn(String.Format("An exception of type {0} occurred while loading subfolders for a directory '{1}'. The exception message is: {2}", ex.GetType().Name, currentDir, ex.Message), "SharePointClient", 778, CSContext.Current.SettingsID);
                    return false;
                }
                // subfolders.Count()>0 means, that the folder already exists
                return !subfolders.Any();
            }
        }

        [Obsolete("Use sharepoint_v2_listItem", true)]
        public SPListItem Update(SPList list, SPListItem item)
        {
            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                SP.List splist = clientContext.ToList(list.Id);
                SP.ListItem listItem = clientContext.ToList(list.Id).GetItemById(item.Id);
                clientContext.Load(listItem);
                foreach (var field in item.Fields)
                {
                    listItem[field.Key] = field.Value;
                }
                clientContext.ValidateOnClient = true;
                listItem.Update();
                clientContext.Load(listItem);
                var fieldsQuery = clientContext.LoadQuery(splist.Fields.Where(field => !field.Hidden && field.Group != "_Hidden"));
                clientContext.ExecuteQuery();
                cacheService.RemoveByTags(new[] { GetTag(list.Id) }, CacheScope.Context | CacheScope.Process);
                return new SPListItem(listItem, fieldsQuery.ToList());
            }
        }

        [Obsolete("Use sharepoint_v2_listItem", true)]
        public void Delete(SPList list, SPListItem item)
        {
            cacheService.RemoveByTags(new[] { GetTag(list.Id) }, CacheScope.Context | CacheScope.Process);

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                SP.List spList = clientContext.ToList(list.Id);
                SP.ListItem spItem = clientContext.ToList(list.Id).GetItemById(item.Id);
                spItem.DeleteObject();
                clientContext.ExecuteQuery();
            }
        }

        internal static string GetTag(Guid listId)
        {
            return string.Concat("sharepoint_v1_listItem.List", listId);
        }

        /// <summary>
        /// Returns an array of Expression used in ClientContext.LoadQuery to retrieve 
        /// the specified field data from a ListItem.
        /// </summary>
        private static Expression<Func<ListItem, object>>[] CreateListItemLoadExpressions(IEnumerable<string> viewFields)
        {
            var expressions = new List<Expression<Func<ListItem, object>>>
                {
                    listItem => listItem.Id,
                    listItem => listItem.ContentType,
                    listItem => listItem["Author"]
                };
            foreach (string viewFieldEntry in viewFields)
            {
                string fieldInternalName = viewFieldEntry;
                Expression<Func<ListItem, object>> retrieveFieldValuesAsHtml = listItem => listItem.FieldValuesAsHtml[fieldInternalName];
                expressions.Add(retrieveFieldValuesAsHtml);

                Expression<Func<ListItem, object>> retrieveFieldValuesForEdit = listItem => listItem.FieldValuesForEdit[fieldInternalName];
                expressions.Add(retrieveFieldValuesForEdit);

                Expression<Func<ListItem, object>> retrieveFieldValuesAsText = listItem => listItem.FieldValuesAsText[fieldInternalName];
                expressions.Add(retrieveFieldValuesAsText);

                Expression<Func<ListItem, object>> retrieveFieldValues = listItem => listItem[fieldInternalName];
                expressions.Add(retrieveFieldValues);
            }
            return expressions.ToArray();
        }
    }
}
