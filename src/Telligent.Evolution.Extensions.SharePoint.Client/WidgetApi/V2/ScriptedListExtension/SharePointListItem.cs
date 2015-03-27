using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class SharePointListItemExtension : IScriptedContentFragmentExtension, ITranslatablePlugin
    {
        internal static class Translations
        {
            public const string CannotBeCreated = "listItem_cannot_be_created";
            public const string CannotBeUpdated = "listItem_cannot_be_updated";
            public const string InvalidCreateOptions = "listItem_invalid_createoptions";
            public const string InvalidUpdateOptions = "listItem_invalid_updateoptions";
            public const string InvalidId = "listItem_invalid_id";
            public const string InvalidListId = "listItem_invalid_libraryid";
            public const string ListNotFound = "listItem_library_notfound";
            public const string NotFound = "listItem_notfound";
            public const string NotFoundBecauseDeleted = "listItem_notfound_because_deleted";
            public const string UnknownError = "listItem_unknown_error";
        }

        private ITranslatablePluginController translationController;

        #region IScriptedContentFragmentExtension Members

        public string ExtensionName
        {
            get { return "sharepoint_v2_listItem"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointListItem>(); }
        }

        public string Name
        {
            get { return "SharePoint List Item Extension (sharepoint_v2_listItem)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to use the SharePoint Client Object Model."; }
        }

        public void Initialize() { }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set(Translations.CannotBeCreated, "The list item can not be created.");
                t.Set(Translations.CannotBeUpdated, "The list item can not be updated.");
                t.Set(Translations.InvalidCreateOptions, "A list item cannot be created, because specified options are invalid.");
                t.Set(Translations.InvalidUpdateOptions, "The list item cannot be updated, because specified options are invalid.");
                t.Set(Translations.InvalidId, "The list item Id is invalid.");
                t.Set(Translations.InvalidListId, "The list Id is invalid.");
                t.Set(Translations.ListNotFound, "The list cannot be found.");
                t.Set(Translations.NotFound, "The list item cannot be found.");
                t.Set(Translations.NotFoundBecauseDeleted, "The list item has been already deleted.");
                t.Set(Translations.UnknownError, "An error has been occurred, please try again or contact your administrator.");

                return new[] { t };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        internal string Translate(string key)
        {
            return translationController.GetLanguageResourceValue(key);
        }
    }

    public interface ISharePointListItem
    {
        SPListItem Current { get; }

        SPListItem Get(Guid contentId);
        SPListItem Get(string url, Guid listId, string listItemId);

        PagedList<SPListItem> List(Guid listId);
        PagedList<SPListItem> List(Guid listId, IDictionary options);

        SPListItem Create(Guid listId);
        SPListItem Create(Guid listId, IDictionary options);

        SPListItem Update(Guid contentId, IDictionary options);
        SPListItem Update(string url, Guid listId, int itemId, IDictionary options);

        AdditionalInfo Delete(Guid contentId);
        AdditionalInfo Delete(string url, Guid listId, string itemIds);

        bool CanEdit(Guid contentId);
        bool CanEdit(string url, Guid listId, string listItemId);

        [Obsolete("Use sharepoint_v1_folder extension. This method will be removed in the next release.")]
        SP.Folder NewFolder(string url, Guid listId, string folderName, string currentDir);

        [Obsolete("Use sharepoint_v1_folder extension. This method will be removed in the next release.")]
        bool IsFolderValid(string url, Guid listId, string folderName, string currentDir);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointListItem : ISharePointListItem
    {
        private static readonly SharePointListItemExtension Plugin = PluginManager.Get<SharePointListItemExtension>().FirstOrDefault();
        private static readonly IListItemDataService listItemDataService = ServiceLocator.Get<IListItemDataService>();

        private readonly ICredentialsManager credentials;

        internal SharePointListItem()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        internal SharePointListItem(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        #region Get

        public SPListItem Current
        {
            get
            {
                try
                {
                    var itemId = SPCoreService.Context.ListItemId;
                    if (itemId != Guid.Empty)
                    {
                        var listId = EnsureListId(itemId);
                        if (listId != Guid.Empty)
                            return PublicApi.ListItems.Get(listId, new SPListItemGetOptions(itemId));
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Current property. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
                return null;
            }
        }

        public SPListItem Get(Guid contentId)
        {
            var item = new SPListItem();

            try
            {
                item = PublicApi.ListItems.Get(EnsureListId(contentId), new SPListItemGetOptions(contentId));
            }
            catch (InvalidOperationException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidId)));
            }
            catch (SPInternalException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.NotFound)));
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Get() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.UnknownError)));
            }

            return item;
        }

        public SPListItem Get(string url, Guid listId, string listItemId)
        {
            var item = new SPListItem();
            SPListItemGetOptions options = null;

            int itemId;
            if (int.TryParse(listItemId, out itemId))
            {
                options = new SPListItemGetOptions(itemId);
            }

            Guid uniqueId;
            if (Guid.TryParse(listItemId, out uniqueId))
            {
                options = new SPListItemGetOptions(uniqueId);
            }

            if (options != null && !item.Errors.Any())
            {
                options.Url = url;
                try
                {
                    item = PublicApi.ListItems.Get(listId, options);
                }
                catch (InvalidOperationException ex)
                {
                    item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidId)));
                }
                catch (SPInternalException ex)
                {
                    item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.NotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Get() method for SPWebUrl: {1} ListId: {2} ListItemId: {3}. The exception message is: {4}", ex.GetType(), url, listId, listItemId, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.UnknownError)));
                }
            }
            else
            {
                item.Errors.Add(new Error(typeof(ArgumentException).ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidId)));
            }

            return item;
        }

        #endregion

        #region List

        public PagedList<SPListItem> List(Guid listId)
        {
            var items = new PagedList<SPListItem>();

            try
            {
                items = PublicApi.ListItems.List(listId);
            }
            catch (InvalidOperationException ex)
            {
                items.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidListId)));
            }
            catch (SPInternalException ex)
            {
                items.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.ListNotFound)));
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.List() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
                items.Errors.Add(new Error(ex.GetType().ToString(), ex.Message));
            }

            return items;
        }

        public PagedList<SPListItem> List(Guid listId,
            [Documentation(Name = "Url", Type = typeof(string)),
            Documentation(Name = "PageIndex", Type = typeof(int)),
            Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "SortBy", Type = typeof(string)),
            Documentation(Name = "SortOrder", Type = typeof(string)),
            Documentation(Name = "ViewFields", Type = typeof(List<string>)),
            Documentation(Name = "ViewQuery", Type = typeof(string))]
            IDictionary options)
        {
            if (options == null)
            {
                return List(listId);
            }

            var items = new PagedList<SPListItem>();

            try
            {
                items = PublicApi.ListItems.List(listId, ProcessListOptions(options));
            }
            catch (InvalidOperationException ex)
            {
                items.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidListId)));
            }
            catch (SPInternalException ex)
            {
                items.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.ListNotFound)));
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.List() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
                items.Errors.Add(new Error(ex.GetType().ToString(), ex.Message));
            }

            return items;
        }

        #endregion

        #region Create

        public SPListItem Create(Guid listId)
        {
            var item = new SPListItem();

            try
            {
                item = PublicApi.ListItems.Create(listId, new SPListItemCreateOptions());
            }
            catch (InvalidOperationException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidCreateOptions)));
            }
            catch (SPInternalException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.CannotBeCreated)));
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Create() method while creating a new listItem in ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.UnknownError)));
            }

            return item;
        }

        public SPListItem Create(Guid listId,
            [Documentation(Name = "Url", Type = typeof(string)),
            Documentation(Name = "Name", Type = typeof(string)),
            Documentation(Name = "FolderUrl", Type = typeof(string)),
            Documentation(Name = "IsFolder", Type = typeof(bool))]
            IDictionary options)
        {
            if (options == null)
            {
                return Create(listId);
            }

            var item = new SPListItem();

            try
            {
                var createItemOptions = ProcessCreateOptions(options);
                item = PublicApi.ListItems.Create(listId, createItemOptions);
            }
            catch (InvalidOperationException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidCreateOptions)));
            }
            catch (SPInternalException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.CannotBeCreated)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Create() method while creating a new listItem in ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.UnknownError)));
            }

            return item;
        }

        #endregion

        #region Update

        public SPListItem Update(Guid contentId,
            [Documentation(Name = "Fields", Description = "A collection of field names and values.", Type = typeof(IDictionary))]
            IDictionary options)
        {
            var item = new SPListItem();

            try
            {
                if (options["Fields"] is IDictionary)
                {
                    item = PublicApi.ListItems.Update(EnsureListId(contentId), new SPListItemUpdateOptions(contentId, (IDictionary)options["Fields"]));
                }
            }
            catch (InvalidOperationException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidUpdateOptions)));
            }
            catch (SPInternalException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.CannotBeUpdated)));
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Update() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.UnknownError)));
            }

            return item;
        }

        public SPListItem Update(string url, Guid listId, int itemId,
            [Documentation(Name = "Fields", Description = "A collection of field names and values.", Type = typeof(IDictionary))]
            IDictionary options)
        {
            var item = new SPListItem();

            try
            {
                item = PublicApi.ListItems.Update(listId, new SPListItemUpdateOptions(itemId, options) { Url = url });

                if (options["Fields"] is IDictionary)
                {
                    item = PublicApi.ListItems.Update(listId, new SPListItemUpdateOptions(itemId, (IDictionary)options["Fields"]) { Url = url });
                }
            }
            catch (InvalidOperationException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidUpdateOptions)));
            }
            catch (SPInternalException ex)
            {
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.CannotBeUpdated)));
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Update() method ContentId: {1} ListId: {2} SPWebUrl: '{3}'. The exception message is: {4}", ex.GetType(), itemId, listId, url, ex.Message);
                SPLog.UnKnownError(ex, message);
                item.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.UnknownError)));
            }

            return item;
        }

        #endregion

        #region Delete

        public AdditionalInfo Delete(Guid contentId)
        {
            var deleteInfo = new AdditionalInfo();

            try
            {
                PublicApi.ListItems.Delete(EnsureListId(contentId), new SPListItemDeleteOptions(new[] { contentId }));
            }
            catch (InvalidOperationException ex)
            {
                deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidId)));
            }
            catch (SPInternalException ex)
            {
                deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.NotFoundBecauseDeleted)));
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Delete() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.UnknownError)));
            }

            return deleteInfo;
        }

        public AdditionalInfo Delete(string url, Guid listId,
            [Documentation(Name = "itemIds", Description = "Comma delimited list of ListItem IDs or UniqueIds", Type = typeof(string))] 
            string itemIds)
        {
            var deleteInfo = new AdditionalInfo();
            var counterIds = new List<int>();
            var uniqueIds = new List<Guid>();
            var ids = itemIds.Split(',');

            foreach (var contentId in ids)
            {
                int counterId;
                Guid uniqueId;

                if (Guid.TryParse(contentId, out uniqueId))
                {
                    uniqueIds.Add(uniqueId);
                }
                else if (int.TryParse(contentId, out counterId))
                {
                    counterIds.Add(counterId);
                }
                else
                {
                    deleteInfo.Errors.Add(new Error(typeof(ArgumentException).ToString(), SharePointListItemExtension.Translations.InvalidListId));
                }
            }

            if (!deleteInfo.Errors.Any())
            {
                try
                {
                    PublicApi.ListItems.Delete(listId, new SPListItemDeleteOptions(counterIds, uniqueIds) { Url = url });
                }
                catch (InvalidOperationException ex)
                {
                    deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.InvalidId)));
                }
                catch (SPInternalException ex)
                {
                    deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.NotFoundBecauseDeleted)));
                }
                catch (Exception ex)
                {
                    var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.Delete() method itemIds: {1}, ListId: {2} Url: {3}. The exception message is: {4}", ex.GetType(), itemIds, listId, url, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(SharePointListItemExtension.Translations.UnknownError)));
                }
            }

            return deleteInfo;
        }

        #endregion

        #region CanEdit

        public bool CanEdit(Guid contentId)
        {
            var canEdit = false;

            try
            {
                canEdit = PublicApi.ListItems.CanEdit(EnsureListId(contentId), new SPListItemGetOptions(contentId));
            }
            catch (InvalidOperationException) { }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.CanEdit() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            return canEdit;
        }

        public bool CanEdit(string url, Guid listId, string listItemId)
        {
            var canEdit = false;

            try
            {
                SPListItemGetOptions options = null;

                int itemId;
                if (int.TryParse(listItemId, out itemId))
                {
                    options = new SPListItemGetOptions(itemId);
                }

                Guid uniqueId;
                if (Guid.TryParse(listItemId, out uniqueId))
                {
                    options = new SPListItemGetOptions(uniqueId);
                }

                if (options == null) throw new Exception("ListItemId cannot be empty.");

                options.Url = url;
                canEdit = PublicApi.ListItems.CanEdit(listId, options);
            }
            catch (InvalidOperationException) { }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointListItem.CanEdit() method ListItemId: {1} ListId: {2} Url: {3}. The exception message is: {4}", ex.GetType(), listItemId, listId, url, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            return canEdit;
        }

        private Guid EnsureListId(Guid contentId)
        {
            var listId = SPCoreService.Context.ListId;
            if (listId != Guid.Empty) return listId;

            var itemBase = listItemDataService.Get(contentId);
            if (itemBase != null)
            {
                listId = itemBase.ApplicationId;
            }
            return listId;
        }

        #endregion

        #region Obsolete

        [Obsolete("Use sharepoint_v1_folder extension. This method will be removed in the next release.")]
        public SP.Folder NewFolder(string url, Guid listId, string folderName, string currentDir)
        {
            if (!IsFolderValid(url, listId, folderName, currentDir))
                return null;

            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {
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
                    SPLog.FileNotFound(ex, "Error occurred while creating a new folder with a name '{0}' for a directory '{1}'.", folderName, currentDir);
                    return null;
                }

                CacheService.RemoveByTags(new[] { Documents.Tag(listId), Folders.Tag(listId) }, CacheScope.All);

                return newFolder;
            }
        }

        [Obsolete("Use sharepoint_v1_folder extension. This method will be removed in the next release.")]
        public bool IsFolderValid(string url, Guid listId, string folderName, string currentDir)
        {
            folderName = folderName.Trim();
            if (String.IsNullOrEmpty(folderName))
            {
                return false;
            }

            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {
                SP.Folder parentFolder = clientContext.Web.GetFolderByServerRelativeUrl(currentDir);
                clientContext.Load(parentFolder);
                var subfolders = clientContext.LoadQuery(parentFolder.Folders.Where(folder => folder.Name == folderName));
                try
                {
                    clientContext.ExecuteQuery();
                }
                catch (Exception ex)
                {
                    SPLog.FileNotFound(ex, "Coult not load subfolders for a directory '{0}'", currentDir);
                    return false;
                }

                return subfolders.Any();
            }
        }

        #endregion

        #region Utility

        private static SPListItemCollectionOptions ProcessListOptions(IDictionary options)
        {
            var listItemsOptions = new SPListItemCollectionOptions();

            if (options["Url"] != null && !string.IsNullOrEmpty(options["Url"].ToString()))
            {
                listItemsOptions.Url = options["Url"].ToString();
            }

            int pageIndex;
            if (options["PageIndex"] != null && int.TryParse(options["PageIndex"].ToString(), out pageIndex))
            {
                listItemsOptions.PageIndex = pageIndex;
            }

            int pageSize;
            if (options["PageSize"] != null && int.TryParse(options["PageSize"].ToString(), out pageSize))
            {
                listItemsOptions.PageSize = pageSize;
            }

            if (options["SortBy"] != null && !string.IsNullOrEmpty(options["SortBy"].ToString()))
            {
                listItemsOptions.SortBy = options["SortBy"].ToString();
            }

            var sortOrder = SortOrder.Ascending;
            if (options["SortOrder"] != null && String.Compare(options["SortOrder"].ToString(), "Descending", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                sortOrder = SortOrder.Descending;
            }
            listItemsOptions.SortOrder = sortOrder;

            if (options["ViewFields"] is IEnumerable<string>)
            {
                listItemsOptions.ViewFields = new List<string>((IEnumerable<string>)options["ViewFields"]);
            }

            if (options["ViewQuery"] != null && !string.IsNullOrEmpty(options["ViewQuery"].ToString()))
            {
                listItemsOptions.ViewQuery = options["ViewQuery"].ToString();
            }

            return listItemsOptions;
        }

        private static SPListItemCreateOptions ProcessCreateOptions(IDictionary options)
        {
            var itemCreateOptions = new SPListItemCreateOptions();

            if (options["Url"] != null)
            {
                itemCreateOptions.Url = options["Url"].ToString();
            }

            if (options["Name"] != null)
            {
                itemCreateOptions.Name = options["Name"].ToString();
            }

            if (options["FolderUrl"] != null)
            {
                itemCreateOptions.FolderUrl = options["FolderUrl"].ToString();
            }

            bool isFolder;
            if (options["IsFolder"] != null && bool.TryParse(options["IsFolder"].ToString(), out isFolder))
            {
                itemCreateOptions.IsFolder = isFolder;
            }

            return itemCreateOptions;
        }

        #endregion
    }
}
